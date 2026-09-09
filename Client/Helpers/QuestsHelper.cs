using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI.DragAndDrop;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ZGFueDkx.ZGCLib.Config;
using ZGFueDkx.ZGCLib.helpers;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (https://github.com/danx91/AllQuestsCheckmarks) - GPL-3.0
    // Extended with MoreCheckmarks (GPLv3, TommySoucy) information dimensions:
    // hideout upgrade materials, wishlist, barter trades and crafting recipes.
    internal static class QuestsHelper
    {
        public static readonly Color DEFAULT_COLOR = new(1, 1, 1, 0.706f);
        public static readonly string COLLECTOR_ID = "5c51aac186f77432ea65c552";
        public static readonly List<MongoID> SPECIAL_BLACKLIST =
        [
            "5991b51486f77447b112d44f", //MS2000 Marker
            "5ac78a9b86f7741cca0bbd8d", //Signal Jammer
            "5b4391a586f7745321235ab2" //Wi-Fi Camera
        ];
        public static readonly List<MongoID> TRUST_REGAIN_QUESTS =
        [
            //Lightkeeper
            "626148251ed3bb5bcc5bd9ed", //Make Amends - Buyout
            "6261482fa4eb80027c4f2e11", //Make Amends - Equipment
            "6391d90f4ed9512be67647df", //Make Amends
            //Fence
            "61e6e5e0f5b9633f6719ed95", //Compensation for Damage - Trust
            "61e6e60223374d168a4576a6", //Compensation for Damage - Wager
            "61e6e615eea2935bc018a2c5", //Compensation for Damage - Barkeep
            "61e6e621bfeab00251576265", //Compensation for Damage - Collection
            //Chemical - Part 4
            "59ca1a6286f774509a270942", // No Offence (Prapor)
            "59c93e8e86f7742a406989c4", // Loyalty Buyout (Skier)
            "59c9392986f7742f6923add2", // Trust Regain (Therapist)
        ];

        // Checkmark arbitration ladder (highest priority first):
        //   Fulfilled -> Active -> Hideout(need) -> Future/Collector -> Wishlist -> Barter -> Craft
        //   -> Hideout(fulfilled) -> Squad -> Fir -> None
        // The new dimensions are merged into the AQC ladder as per the fusion plan:
        // Hideout sits right after Active; Wishlist/Barter/Craft sit right before Fir.
        public enum ECheckmarkStatus
        {
            None = 0,
            Fir = 1,
            Active = 2,
            Future = 3,
            Squad = 4,
            Fulfilled = 5,
            Collector = 6,
            Hideout = 7,            // Materials missing for a hideout upgrade
            HideoutFulfilled = 8,   // Have enough materials for (at least) the next upgrade
            Wishlist = 9,           // Item is on the wishlist
            Barter = 10,            // Item is used in a trader barter deal
            Craft = 11              // Item is an ingredient of a crafting recipe
        }

        public class CurrentQuest(QuestTemplate template, ConditionItem condition)
        {
            public QuestTemplate Template = template;
            public ConditionItem Condition = condition;
        }

        public static bool IsNeededForActiveOrFutureQuests(Item item, out QuestsData.ItemData quests)
        {
            return QuestsData.QuestItemsByItemId.TryGetValue(item.TemplateId, out quests) &&
                   (item.MarkedAsSpawnedInSession && quests.Total > 0 || quests.NonFir > 0);
        }

        public static bool GetActiveQuestsWithItem(Profile profile, Item item, out Dictionary<MongoID, CurrentQuest> activeQuests,
            out Dictionary<MongoID, CurrentQuest> fulfilled)
        {
            bool activeNonFir = false;
            activeQuests = [];
            fulfilled = [];

            foreach (QuestDataClass questDataClass in profile.QuestsData)
            {
                if (questDataClass.Template is null ||
                   (questDataClass.Status != EQuestStatus.Started && questDataClass.Status != EQuestStatus.AvailableForFinish))
                {
                    continue;
                }

                foreach (KeyValuePair<EQuestStatus, ConditionCollection> keyValuePair in questDataClass.Template.Conditions)
                {
                    keyValuePair.Deconstruct(out _, out ConditionCollection conditions);

                    ConditionItem? tmpCondition = null;
                    bool isFulfilled = false;

                    foreach (Condition condition in conditions)
                    {
                        if (condition is not ConditionItem conditionItem || !conditionItem.target.Contains(item.StringTemplateId))
                        {
                            continue;
                        }

                        isFulfilled = questDataClass.CompletedConditions.Contains(condition.id);
                        tmpCondition = conditionItem;

                        if (conditionItem is ConditionHandoverItem)
                        {
                            break;
                        }
                    }

                    if (tmpCondition is null)
                    {
                        continue;
                    }

                    if (isFulfilled)
                    {
                        fulfilled.Add(questDataClass.Template.Id, new CurrentQuest(questDataClass.Template, tmpCondition));
                    }
                    else
                    {
                        activeQuests.Add(questDataClass.Template.Id, new CurrentQuest(questDataClass.Template, tmpCondition));

                        if (!tmpCondition.onlyFoundInRaid)
                        {
                            activeNonFir = true;
                        }
                    }

                    break;
                }
            }

            if (activeQuests.Count == 0)
            {
                return false;
            }

            if (item.QuestItem)
            {
                return true;
            }

            if (item is not Weapon weapon)
            {
                return activeNonFir || item.MarkedAsSpawnedInSession;
            }

            foreach (KeyValuePair<MongoID, CurrentQuest> quest in new Dictionary<MongoID, CurrentQuest>(activeQuests))
            {
                if (quest.Value.Condition is not ConditionWeaponAssembly conditionWeaponAssembly)
                {
                    continue;
                }

                if (Inventory.IsWeaponFitsCondition(weapon, conditionWeaponAssembly))
                {
                    return true;
                }

                activeQuests.Remove(quest.Key);
            }

            return activeNonFir || item.MarkedAsSpawnedInSession;
        }

        /// <summary>
        /// Arbitrates the single checkmark status from every information source.
        /// All boolean flags are expected to be already gated by their corresponding Settings toggles
        /// (the caller decides whether a dimension is enabled/visible); this method only decides priority.
        /// </summary>
        /// <param name="active">Needed for an active (started) quest.</param>
        /// <param name="future">Needed for a future (not yet started) quest.</param>
        /// <param name="squad">Needed by a Fika squad member.</param>
        /// <param name="fir">Item was found in raid (FiR fallback marker).</param>
        /// <param name="enough">Stash already holds enough items for all active/future quests.</param>
        /// <param name="collector">The only future quest needing it is the Collector quest.</param>
        /// <param name="hideoutNeeded">Materials are still missing for a hideout upgrade.</param>
        /// <param name="hideoutFulfilled">Enough materials are in stash for (at least) the next upgrade.</param>
        /// <param name="wishlist">Item is on the player's wishlist.</param>
        /// <param name="barter">Item is used as a trader barter currency.</param>
        /// <param name="craft">Item is an ingredient of a hideout crafting recipe.</param>
        public static ECheckmarkStatus GetCheckmarkStatus(bool active, bool future, bool squad, bool fir, bool enough, bool collector,
            bool hideoutNeeded = false, bool hideoutFulfilled = false, bool wishlist = false, bool barter = false, bool craft = false)
        {
            // Already have enough for every active/future quest
            if (enough && (active || future))
            {
                if (Settings.HideFulfilled!.Value && RaidUtils.IsInRaid())
                {
                    return squad ? ECheckmarkStatus.Squad : fir ? ECheckmarkStatus.Fir : ECheckmarkStatus.None;
                }

                if (Settings.MarkEnoughItems!.Value)
                {
                    return squad ? ECheckmarkStatus.Squad : ECheckmarkStatus.Fulfilled;
                }
            }

            // Active quest always wins
            if (active)
            {
                return ECheckmarkStatus.Active;
            }

            // Hideout upgrade still missing materials - actionable right now, right after active quests
            if (hideoutNeeded)
            {
                return ECheckmarkStatus.Hideout;
            }

            // Future quest (or Collector-only)
            if (future)
            {
                return collector ? ECheckmarkStatus.Collector : ECheckmarkStatus.Future;
            }

            // Wishlist - player explicitly wants this item
            if (wishlist)
            {
                return ECheckmarkStatus.Wishlist;
            }

            // Barter trade value
            if (barter)
            {
                return ECheckmarkStatus.Barter;
            }

            // Crafting ingredient
            if (craft)
            {
                return ECheckmarkStatus.Craft;
            }

            // Hideout materials already satisfied - "ready to upgrade", low urgency,
            // so every remaining "need" signal above outranks it
            if (hideoutFulfilled)
            {
                return ECheckmarkStatus.HideoutFulfilled;
            }

            if (squad)
            {
                return ECheckmarkStatus.Squad;
            }

            if (fir)
            {
                return ECheckmarkStatus.Fir;
            }

            return ECheckmarkStatus.None;
        }

        /// <summary>
        /// Resolves the configured checkmark color for a status. Color scheme follows AQC;
        /// the merged MoreCheckmarks dimensions get their own F12-configurable colors.
        /// </summary>
        /// <param name="status">Arbitrated checkmark status.</param>
        /// <param name="itemFir">Whether the item itself was found in raid (affects Future color).</param>
        public static Color GetCheckmarkColor(ECheckmarkStatus status, bool itemFir)
        {
            switch (status)
            {
                case ECheckmarkStatus.Fir:
                    return DEFAULT_COLOR;
                case ECheckmarkStatus.Active:
                    return Settings.UseCustomQuestColor!.Value ? Settings.CustomQuestColor!.GetValue() : DEFAULT_COLOR;
                case ECheckmarkStatus.Future:
                    return itemFir ? Settings.CheckmarkColor!.GetValue() : Settings.NonFirColor!.GetValue();
                case ECheckmarkStatus.Squad:
                    return Settings.SquadColor!.GetValue();
                case ECheckmarkStatus.Fulfilled:
                    return Settings.EnoughItemsColor!.GetValue();
                case ECheckmarkStatus.Collector:
                    return Settings.CollectorColor!.GetValue();
                case ECheckmarkStatus.Hideout:
                    return Settings.HideoutColor!.GetValue();
                case ECheckmarkStatus.HideoutFulfilled:
                    return Settings.HideoutFulfilledColor!.GetValue();
                case ECheckmarkStatus.Wishlist:
                    return Settings.WishlistColor!.GetValue();
                case ECheckmarkStatus.Barter:
                    return Settings.BarterColor!.GetValue();
                case ECheckmarkStatus.Craft:
                    return Settings.CraftColor!.GetValue();
                default:
                    return DEFAULT_COLOR;
            }
        }

        public static void SetCheckmark(QuestItemViewPanel panel, Image image, Sprite sprite, Color color)
        {
            panel.ShowGameObject();
            image.sprite = sprite;
            image.color = color;
        }
    }
}
