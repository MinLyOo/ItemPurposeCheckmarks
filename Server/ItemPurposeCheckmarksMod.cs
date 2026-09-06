using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Quest;
using SPTarkov.Server.Core.Helpers.Traders;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using Path = System.IO.Path;

namespace ItemPurposeCheckmarks
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0).
    // Extended with MoreCheckmarks (MIT, TommySoucy) server routes:
    // trader assorts (barter), hideout productions (craft) and quest-exclusion config.
    [Injectable]
    public class ItemPurposeCheckmarksMod(
        HttpResponseUtil httpResponseUtil,
        QuestConfig questConfig,
        ProfileHelper profileHelper,
        QuestHelper questHelper,
        TraderHelper traderHelper,
        FenceService fenceService,
        HideoutTable hideoutTable,
        ISptLogger<ItemPurposeCheckmarksMod> logger
    )
    {
        private readonly HttpResponseUtil _httpResponseUtil = httpResponseUtil;
        private readonly QuestConfig _questConfig = questConfig;
        private readonly ProfileHelper _profileHelper = profileHelper;
        private readonly QuestHelper _questHelper = questHelper;
        private readonly TraderHelper _traderHelper = traderHelper;
        private readonly FenceService _fenceService = fenceService;
        private readonly HideoutTable _hideoutTable = hideoutTable;
        private readonly ISptLogger<ItemPurposeCheckmarksMod> _logger = logger;

        private readonly string _modFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        private readonly ServerConfig _config = ServerConfig.LoadOrCreate(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            msg => logger.Error(msg)
        );

        // quest-id-reference.txt is best-effort diagnostics for config.json editing.
        public void WriteQuestReference()
        {
            if (string.IsNullOrEmpty(_modFolder))
            {
                return;
            }

            try
            {
                Dictionary<MongoId, string> traderNames = BuildTraderNameMap();
                List<string> lines =
                [
                    "# ItemPurposeCheckmarks quest reference - auto-generated on server start.",
                    "# Format: Quest Name [Trader] = questId",
                    "# Paste a questId into excludedQuestIds in config.json to hide it.",
                    ""
                ];

                foreach (Quest quest in _questHelper.GetQuestsFromDb())
                {
                    string name = string.IsNullOrEmpty(quest.QuestName) ? quest.Name : quest.QuestName;
                    string trader = traderNames.TryGetValue(quest.TraderId, out string? traderName) ? traderName : quest.TraderId.ToString();
                    lines.Add($"{name} [{trader}] = {quest.Id}");
                }

                lines.Sort(StringComparer.OrdinalIgnoreCase);
                File.WriteAllLines(Path.Combine(_modFolder, "quest-id-reference.txt"), lines);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to write quest-id-reference.txt: {ex.Message}");
            }
        }

        public ValueTask<string> GetAllQuests(MongoId profileId)
        {
            PmcData? profile = _profileHelper.GetPmcProfile(profileId);

            if (profile is null || profile.Info is not Info profileInfo || profile.Quests is null)
            {
                _logger.Error($"Failed to retrieve user profile or info: {profileId}");
                return new ValueTask<string>(_httpResponseUtil.EmptyResponse());
            }

            List<QuestJson> quests = [];
            List<Quest> allQuests = _questHelper.GetQuestsFromDb();

            foreach (Quest quest in allQuests)
            {
                if (IsQuestExcluded(quest.Id))
                {
                    continue;
                }

                if (!_questHelper.ShowEventQuestToPlayer(quest.Id) || !IsQuestForGameType(quest.Id, profileInfo.GameVersion!, _questConfig))
                {
                    quests.Add(new QuestJson(quest)
                    {
                        IsUnreachable = true
                    });
                    continue;
                }

                if (IsOtherFaction(profile, quest.Id, _questConfig))
                {
                    continue;
                }

                QuestStatusEnum questStatus = profile.GetQuestStatus(quest.Id);
                if (questStatus is
                    QuestStatusEnum.AvailableForFinish or
                    QuestStatusEnum.Success or
                    QuestStatusEnum.Fail or
                    QuestStatusEnum.FailRestartable or
                    QuestStatusEnum.MarkedAsFailed or
                    QuestStatusEnum.Expired)
                {
                    continue;
                }

                quests.Add(new QuestJson(quest));
            }

            foreach (RepeatableQuest quest in GetRepeatableQuests(profile))
            {
                if (profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                {
                    quests.Add(new QuestJson(quest));
                }
            }

            return new ValueTask<string>(_httpResponseUtil.NoBody(quests));
        }

        public ValueTask<string> HandleGetActiveQuests(List<MongoId> info)
        {
            Dictionary<MongoId, List<QuestStripped>> data = [];

            foreach (MongoId profileId in info)
            {
                try
                {
                    data[profileId] = GetActiveQuests(profileId);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error retrieving active quests for profile {profileId}: {ex}");
                    data[profileId] = [];
                }
            }

            return new ValueTask<string>(_httpResponseUtil.NoBody(data));
        }

        private List<QuestStripped> GetActiveQuests(MongoId profileId)
        {
            List<QuestStripped> quests = [];
            PmcData? profile = _profileHelper.GetPmcProfile(profileId);

            if (profile is null || profile.Quests is not List<QuestStatus> profileQuests)
            {
                _logger.Error($"Failed to retrieve user profile or info: {profileId}");
                return quests;
            }

            List<Quest> allQuests = _questHelper.GetClientQuests(profileId);

            foreach (Quest quest in allQuests)
            {
                if (IsQuestExcluded(quest.Id))
                {
                    continue;
                }

                QuestStatus? questStatus = profileQuests.Find(q => q.QId == quest.Id);
                if (questStatus?.Status != QuestStatusEnum.Started)
                {
                    continue;
                }

                if (questStatus.CompletedConditions is null || questStatus.CompletedConditions.Count == 0)
                {
                    quests.Add(new QuestStripped(quest));
                    continue;
                }

                List<QuestCondition> newConditions = quest.Conditions.AvailableForFinish!.FindAll(c => questStatus.CompletedConditions.Contains(c.Id));

                if (newConditions.Count == 0)
                {
                    continue;
                }

                quests.Add(new(quest, availableForFinishCondition: AvailableForFinishCondition.FromQuestConditions(newConditions)));
            }

            foreach (RepeatableQuest quest in GetRepeatableQuests(profile))
            {
                if (profile.GetQuestStatus(quest.Id) == QuestStatusEnum.Started)
                {
                    quests.Add(new QuestStripped(quest));
                }
            }

            return quests;
        }

        // --- Trader assorts (barter deals) ---
        public ValueTask<string> HandleAssorts(MongoId sessionId)
        {
            try
            {
                return new ValueTask<string>(_httpResponseUtil.NoBody(GetOrderedTraderAssorts(sessionId).Select(x => x.Assort).ToArray()));
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception caught when trying to generate assorts: {ex.Message}");
                return new ValueTask<string>(_httpResponseUtil.NullResponse());
            }
        }

        public ValueTask<string> HandleTraderNames(MongoId sessionId)
        {
            try
            {
                return new ValueTask<string>(_httpResponseUtil.NoBody(GetOrderedTraderAssorts(sessionId).Select(x => x.Name).ToArray()));
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception caught when trying to generate trader names: {ex.Message}");
                return new ValueTask<string>(_httpResponseUtil.NullResponse());
            }
        }

        // --- Hideout productions (crafting recipes) ---
        public ValueTask<string> HandleProductions()
        {
            try
            {
                return new ValueTask<string>(_httpResponseUtil.NoBody(_hideoutTable.Production));
            }
            catch (Exception ex)
            {
                _logger.Error($"Could not get hideout productions: {ex.Message}");
                return new ValueTask<string>(_httpResponseUtil.NullResponse());
            }
        }

        // Traders are iterated in Traders enum order so the client can map names to assorts by index.
        // Fence assorts are generated separately by FenceService.
        private List<(string Name, TraderAssort Assort)> GetOrderedTraderAssorts(MongoId sessionId)
        {
            List<(string Name, TraderAssort Assort)> result = [];
            List<TraderBase> traders = _traderHelper.GetAllTraders(sessionId);
            TraderAssort? fenceAssorts = _fenceService.GetRawFenceAssorts();

            foreach (FieldInfo traderField in typeof(Traders).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (traderField.GetValue(null) is not MongoId traderId)
                {
                    continue;
                }

                if (traderId == Traders.FENCE)
                {
                    if (fenceAssorts is not null)
                    {
                        result.Add(("Fence", fenceAssorts));
                    }

                    continue;
                }

                TraderBase? traderBase = traders.Find(t => t.Id == traderId);
                if (traderBase is null)
                {
                    continue;
                }

                TraderAssort? assort;
                try
                {
                    assort = _traderHelper.GetTraderAssortsByTraderId(traderId);
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to get assorts for trader {traderBase.Nickname}: {ex.Message}");
                    continue;
                }

                if (assort is null)
                {
                    continue;
                }

                result.Add((traderBase.Nickname ?? traderField.Name, assort));
            }

            return result;
        }

        private Dictionary<MongoId, string> BuildTraderNameMap()
        {
            Dictionary<MongoId, string> map = [];

            try
            {
                foreach (FieldInfo traderField in typeof(Traders).GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    if (traderField.GetValue(null) is not MongoId traderId)
                    {
                        continue;
                    }

                    if (traderId == Traders.FENCE)
                    {
                        map[traderId] = "Fence";
                        continue;
                    }

                    TraderBase? trader = _traderHelper.GetTrader(traderId, null);
                    if (!string.IsNullOrEmpty(trader?.Nickname))
                    {
                        map[traderId] = trader!.Nickname!;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to build trader name map: {ex.Message}");
            }

            return map;
        }

        private bool IsQuestExcluded(MongoId questId)
        {
            if (_config.ExcludedQuestIds.Contains(questId.ToString()))
            {
                return true;
            }

            if (_config.HideInactiveEventQuests && !_questHelper.ShowEventQuestToPlayer(questId))
            {
                return true;
            }

            return false;
        }

        private static List<RepeatableQuest> GetRepeatableQuests(PmcData profile)
        {
            List<RepeatableQuest> quests = [];

            if (profile.RepeatableQuests is null)
            {
                return quests;
            }

            foreach (PmcDataRepeatableQuest current in profile.RepeatableQuests)
            {
                if (current.ActiveQuests is null)
                {
                    continue;
                }

                foreach (RepeatableQuest quest in current.ActiveQuests)
                {
                    quests.Add(quest);
                }
            }

            return quests;
        }

        private static bool IsOtherFaction(PmcData profile, MongoId questId, QuestConfig questConfig)
        {
            bool usec = profile.Info!.Side!.Equals("usec", StringComparison.OrdinalIgnoreCase);
            return usec && questConfig.BearOnlyQuests.Contains(questId) ||
                   !usec && questConfig.UsecOnlyQuests.Contains(questId);
        }

        private static bool IsQuestForGameType(MongoId questId, string version, QuestConfig questConfig)
        {
            if (questConfig.ProfileBlacklist.TryGetValue(version, out HashSet<MongoId>? blacklistValue) && blacklistValue.Contains(questId))
            {
                return false;
            }

            if (questConfig.ProfileWhitelist.TryGetValue(questId, out HashSet<string>? whitelistValue) && !whitelistValue.Contains(version))
            {
                return false;
            }

            return true;
        }
    }
}
