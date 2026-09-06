using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using System.Collections.Generic;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on MoreCheckmarks (MIT, TommySoucy) hideout material logic,
    // adapted to SPT 4.1's HideoutRepresentation API.
    internal static class HideoutHelper
    {
        /// <summary>
        /// Result of checking whether an item is needed for any hideout upgrade.
        /// </summary>
        public class HideoutNeed
        {
            public bool FoundNeeded;      // Materials are still missing
            public bool FoundFulfilled;   // Already have enough for (at least) one upgrade
            public int PossessedCount;
            public int RequiredCount;
            public readonly List<HideoutAreaEntry> Areas = [];
        }

        public class HideoutAreaEntry(string areaName, int level, bool fulfilled)
        {
            public string AreaName = areaName;
            public int Level = level;
            public bool Fulfilled = fulfilled;
        }

        /// <summary>
        /// Walks every hideout area's future stage requirements and reports whether
        /// the given item template is one of the required materials.
        /// </summary>
        public static HideoutNeed GetNeeded(MongoID itemTemplateId)
        {
            HideoutNeed result = new();

            HideoutRepresentation? hideout = Singleton<HideoutRepresentation>.Instance;
            if (hideout?.AreaDatas is null)
            {
                return result;
            }

            foreach (AreaData areaData in hideout.AreaDatas)
            {
                if (areaData is null || areaData.Template is null || areaData.NextStage is null)
                {
                    continue;
                }

                // Skip areas that have no future upgrades
                if (areaData.Status == EAreaStatus.NoFutureUpgrades)
                {
                    continue;
                }

                // Collect every future stage the player may reach.
                List<Stage> futureStages = [];

                // If the area is already being constructed/upgraded, the current requirements
                // are in progress, so skip requirement tracking for this area entirely.
                if (areaData.Status is not (EAreaStatus.Constructing or EAreaStatus.Upgrading))
                {
                    int nextLevel = areaData.CurrentStage?.Level + 1 ?? 1;

                    while (areaData.StageAt(nextLevel) is Stage stage && stage.Level != 0)
                    {
                        futureStages.Add(stage);

                        if (!Settings.ShowAllFutureHideoutLevels!.Value)
                        {
                            break;
                        }

                        nextLevel = stage.Level + 1;
                    }
                }

                if (futureStages.Count == 0)
                {
                    continue;
                }

                string areaName = areaData.Template.Name?.Localized() ?? "Unknown area";

                foreach (Stage stage in futureStages)
                {
                    RelatedRequirements? requirements = stage.Requirements;
                    if (requirements is null)
                    {
                        continue;
                    }

                    foreach (Requirement requirement in requirements)
                    {
                        if (requirement is not ItemRequirement itemRequirement)
                        {
                            continue;
                        }

                        if (itemRequirement.TemplateId != itemTemplateId)
                        {
                            continue;
                        }

                        result.RequiredCount += itemRequirement.IntCount;
                        result.PossessedCount = itemRequirement.UserItemsCount;

                        if (itemRequirement.Fulfilled)
                        {
                            if (!result.FoundNeeded && !result.FoundFulfilled)
                            {
                                result.FoundFulfilled = true;
                            }

                            result.Areas.Add(new HideoutAreaEntry(areaName, stage.Level, true));
                        }
                        else
                        {
                            result.FoundNeeded = true;
                            result.Areas.Add(new HideoutAreaEntry(areaName, stage.Level, false));
                        }
                    }
                }
            }

            return result;
        }

        public static bool IsNeeded(MongoID itemTemplateId, Item item)
        {
            HideoutNeed need = GetNeeded(itemTemplateId);
            if (!need.FoundNeeded && !need.FoundFulfilled)
            {
                return false;
            }

            if (Settings.OnlyShowHideoutOnFir!.Value && !item.MarkedAsSpawnedInSession)
            {
                return false;
            }

            return true;
        }
    }
}
