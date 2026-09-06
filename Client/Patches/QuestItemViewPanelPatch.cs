using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using ItemPurposeCheckmarks.Helpers;
using JetBrains.Annotations;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZGFueDkx.ZGCLib.Config;

namespace ItemPurposeCheckmarks.Patches
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0).
    // Extended with MoreCheckmarks (MIT) dimensions: hideout materials, barter trades,
    // crafting recipes and wishlist. All feed the single QuestsHelper checkmark arbiter.
    internal class QuestItemViewPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestItemViewPanel), nameof(QuestItemViewPanel.Show));
        }

        [PatchPrefix]
        static bool Prefix(Profile profile, Item item, [CanBeNull] SimpleTooltip tooltip, QuestItemViewPanel __instance, Image ____questIconImage,
            Sprite ____foundInRaidSprite, Sprite ____questItemSprite, ref string ____tooltipText, ref SimpleTooltip ____tooltip, TextMeshProUGUI ____questItemLabel)
        {
            __instance.HideGameObject();

            if (____questItemLabel != null)
            {
                ____questItemLabel.gameObject.SetActive(item.QuestItem);
                if (item.QuestItem)
                {
                    ____questItemLabel.text = "QUEST ITEM".Localized(null);
                }
            }

            ____tooltip = tooltip;
            ____tooltipText = "";

            if (item.MarkedAsSpawnedInSession)
            {
                ____tooltipText = "Item found in raid".Localized(null) + "\n";
            }

            bool showNonFir = Settings.IncludeNonFir!.Value;
            StashHelper.ItemsCount inStash = StashHelper.GetItemsInStash(item.TemplateId);
            ____tooltipText += string.Format("aqc_in_stash".Localized(null), inStash.Total, inStash.Fir);

            // MoreCheckmarks "On You" line - how many are currently on your character.
            if (Settings.ShowOnYouCount!.Value)
            {
                int onYou = StashHelper.GetOnYouCount(item.TemplateId);
                if (onYou > 0)
                {
                    ____tooltipText += "\n" + string.Format("aqc_on_you".Localized(null), onYou);
                }
            }

            bool neededForActive = false;
            bool neededForFuture = false;
            bool neededForFriend = false;
            bool collectorOnly = true;

            int totalNeededFir = 0;
            int totalNeededNonFir = 0;
            int handedOverFir = 0;
            int handedOverNonFir = 0;

            string activeQuestsTooltip = "";
            string futureQuestsTooltip = "";

            bool useCustomTextColors = Settings.CustomTextColors!.Value;
            string indent = Settings.BulletPoints!.Value ? "  · " : "  ";

            if (QuestsHelper.GetActiveQuestsWithItem(profile, item, out Dictionary<MongoID, QuestsHelper.CurrentQuest> activeQuests,
                out Dictionary<MongoID, QuestsHelper.CurrentQuest> fulfilled))
            {
                string activeColor = Settings.ActiveQuestTextColor!.GetHexColor(!useCustomTextColors);

                foreach (KeyValuePair<MongoID, QuestsHelper.CurrentQuest> keyValuePair in activeQuests)
                {
                    keyValuePair.Deconstruct(out _, out QuestsHelper.CurrentQuest quest);

                    if (showNonFir || item.QuestItem || quest.Condition.onlyFoundInRaid)
                    {
                        if (!neededForActive)
                        {
                            neededForActive = true;
                            activeQuestsTooltip += "\n" + "aqc_active_quests".Localized(null);
                        }
                    }
                    else
                    {
                        continue;
                    }

                    string trader = QuestsData.GetTraderName(quest.Template.Id);
                    activeQuestsTooltip += $"\n{indent}{(string.IsNullOrEmpty(trader) ? "" : trader + ": ")}<color={activeColor}>{quest.Template.Name}</color>: ";

                    if (quest.Condition is ConditionHandoverItem condition
                        && profile.TaskConditionCounters.TryGetValue(condition.id, out TaskConditionCounter counter))
                    {
                        if (condition.onlyFoundInRaid)
                        {
                            handedOverFir += counter.Value;
                            totalNeededFir += (int)condition.value - counter.Value;
                        }
                        else
                        {
                            handedOverNonFir += counter.Value;
                            totalNeededNonFir += (int)condition.value - counter.Value;
                        }

                        activeQuestsTooltip += $"{counter.Value}/{condition.value}";

                        if (showNonFir)
                        {
                            activeQuestsTooltip += " " + (condition.onlyFoundInRaid ? "aqc_fir" : "aqc_nonfir").Localized(null);
                        }
                    }
                    else
                    {
                        activeQuestsTooltip += $"0/{quest.Condition.value}";
                    }
                }
            }

            foreach (KeyValuePair<MongoID, QuestsHelper.CurrentQuest> keyValuePair in fulfilled)
            {
                keyValuePair.Deconstruct(out _, out QuestsHelper.CurrentQuest quest);

                if (quest.Condition.onlyFoundInRaid)
                {
                    handedOverFir += (int)quest.Condition.value;
                }
                else
                {
                    handedOverNonFir += (int)quest.Condition.value;
                }
            }

            if (!Settings.OnlyActiveQuests!.Value && QuestsHelper.IsNeededForActiveOrFutureQuests(item, out QuestsData.ItemData itemData))
            {
                string futureColor = Settings.FutureQuestTextColor!.GetHexColor(!useCustomTextColors);

                totalNeededFir = itemData.Fir - handedOverFir;
                totalNeededNonFir = itemData.NonFir - handedOverNonFir;

                foreach (KeyValuePair<MongoID, QuestsData.QuestValues> quest in itemData.Quests)
                {
                    if (activeQuests.ContainsKey(quest.Key) || fulfilled.ContainsKey(quest.Key))
                    {
                        continue;
                    }

                    if (!neededForFuture)
                    {
                        neededForFuture = true;
                        futureQuestsTooltip = "\n" + "aqc_future_quests".Localized();
                    }

                    if (quest.Key != QuestsHelper.COLLECTOR_ID)
                    {
                        collectorOnly = false;
                    }

                    string questName = quest.Value.LocalizedName.Localized(null);
                    if (questName == quest.Value.LocalizedName)
                    {
                        if (questName.IsNullOrEmpty())
                        {
                            questName = "Unknown Quest";
                        }
                        else
                        {
                            questName = quest.Value.Name;
                        }
                    }

                    string trader = QuestsData.GetTraderName(quest.Key);
                    futureQuestsTooltip += $"\n{indent}{(string.IsNullOrEmpty(trader) ? "" : trader + ": ")}<color={futureColor}>{questName}</color>: {quest.Value.Count.Count}";

                    if (showNonFir)
                    {
                        futureQuestsTooltip += " " + (quest.Value.Count.Fir ? "aqc_fir" : "aqc_nonfir").Localized(null);
                    }

                    // MoreCheckmarks prerequisite count - how many prereqs are still unfinished.
                    if (Settings.ShowPrereqCount!.Value)
                    {
                        int prereq = QuestsData.GetRemainingPrereqCount(quest.Key, profile);
                        if (prereq > 0)
                        {
                            futureQuestsTooltip += string.Format("aqc_prereqs".Localized(null), prereq, prereq > 1 ? (object)"s" : (object)"");
                        }
                    }
                }
            }

            if (totalNeededFir > 0 || showNonFir && totalNeededNonFir > 0)
            {
                ____tooltipText += "\n" + string.Format((showNonFir ? "aqc_total_needed_alt" : "aqc_total_needed").Localized(null),
                       totalNeededFir, totalNeededNonFir);
            }

            ____tooltipText += activeQuestsTooltip + futureQuestsTooltip;

            if (Plugin.isFikaInstalled && Settings.SquadQuests!.Value && SquadQuests.IsNeededForSquadMembers(item, out List<string> members))
            {
                string squadColor = Settings.SquadQuestTextColor!.GetHexColor(!useCustomTextColors);

                neededForFriend = true;
                ____tooltipText += "\n" + "aqc_squad_quests".Localized(null);

                foreach (string nick in members)
                {
                    ____tooltipText += $"\n{indent}<color={squadColor}>{nick}</color>";
                }
            }

            // ===== Merged MoreCheckmarks dimensions =====

            bool hideoutNeeded = false;
            bool hideoutFulfilled = false;

            if (Settings.ShowHideout!.Value)
            {
                HideoutHelper.HideoutNeed hideoutNeed = HideoutHelper.GetNeeded(item.TemplateId);

                if (hideoutNeed.FoundNeeded || hideoutNeed.FoundFulfilled)
                {
                    if (hideoutNeed.FoundNeeded)
                    {
                        hideoutNeeded = true;
                    }
                    else
                    {
                        hideoutFulfilled = true;
                    }

                    if (!Settings.OnlyShowHideoutOnFir!.Value || item.MarkedAsSpawnedInSession)
                    {
                        ____tooltipText += "\n" + string.Format("aqc_hideout".Localized(null), hideoutNeed.TotalPossessed, hideoutNeed.TotalRequired);

                        string hideoutColor = Settings.HideoutColor!.GetHexColor(!useCustomTextColors);
                        string fulfilledColor = Settings.HideoutFulfilledColor!.GetHexColor(!useCustomTextColors);

                        foreach (HideoutHelper.HideoutAreaEntry area in hideoutNeed.Areas)
                        {
                            string color = area.Fulfilled ? fulfilledColor : hideoutColor;
                            ____tooltipText += $"\n{indent}<color={color}>{string.Format("aqc_hideout_entry".Localized(null), area.AreaName, area.Level, area.PossessedCount, area.RequiredCount)}</color>";
                        }
                    }
                }
            }

            bool barter = false;

            if (Settings.ShowBarter!.Value)
            {
                List<List<KeyValuePair<MongoID, int>>> bartersByTrader = BarterHelper.GetBarters(item.TemplateId);
                bool any = false;

                for (int i = 0; i < bartersByTrader.Count; ++i)
                {
                    if (bartersByTrader[i].Count == 0)
                    {
                        continue;
                    }

                    any = true;

                    if (i < BarterHelper.TraderNames.Length)
                    {
                        ____tooltipText += $"\n{indent}{BarterHelper.TraderNames[i]}:";
                    }

                    foreach (KeyValuePair<MongoID, int> offer in bartersByTrader[i])
                    {
                        string productName = Helpers.Utils.GetItemName(offer.Key);
                        ____tooltipText += $"\n{indent}  {string.Format("aqc_barter_for".Localized(null), item.Name, offer.Value, productName)}";
                    }
                }

                if (any)
                {
                    barter = true;
                    ____tooltipText += "\n" + "aqc_barters".Localized(null);
                }
            }

            bool craft = false;

            if (Settings.ShowCraft!.Value && CraftHelper.IsCraftingIngredient(item.TemplateId))
            {
                craft = true;

                MongoID? endProduct = CraftHelper.GetCraftEndProduct(item.TemplateId);
                if (endProduct != null)
                {
                    string productName = Helpers.Utils.GetItemName(endProduct.Value);
                    ____tooltipText += "\n" + "aqc_crafts".Localized(null);
                    ____tooltipText += $"\n{indent}{string.Format("aqc_craft_into".Localized(null), productName)}";
                }
                else
                {
                    ____tooltipText += "\n" + "aqc_crafts".Localized(null);
                }
            }

            bool wishlist = false;

            if (Settings.ShowWishlist!.Value && profile.WishlistManager != null)
            {
                wishlist = profile.WishlistManager.IsInWishlist(item.TemplateId, false, out _);
                if (wishlist)
                {
                    ____tooltipText += "\n" + "aqc_wishlist".Localized(null);
                }
            }

            // ===== Checkmark arbitration =====

            int leftFir = inStash.Fir - totalNeededFir;
            bool enough = leftFir >= 0 && inStash.NonFir + leftFir - totalNeededNonFir >= 0;

            QuestsHelper.ECheckmarkStatus status = QuestsHelper.GetCheckmarkStatus(
                active: neededForActive,
                future: neededForFuture,
                squad: neededForFriend,
                fir: item.MarkedAsSpawnedInSession,
                enough: enough,
                collector: collectorOnly,
                hideoutNeeded: hideoutNeeded,
                hideoutFulfilled: hideoutFulfilled,
                wishlist: wishlist,
                barter: barter,
                craft: craft
            );

            if (status != QuestsHelper.ECheckmarkStatus.None)
            {
                Sprite sprite = status == QuestsHelper.ECheckmarkStatus.Active && !Settings.UseCustomQuestColor!.Value
                    ? ____questItemSprite
                    : ____foundInRaidSprite;

                QuestsHelper.SetCheckmark(__instance, ____questIconImage, sprite, QuestsHelper.GetCheckmarkColor(status, item.MarkedAsSpawnedInSession));
            }

            return false;
        }
    }
}
