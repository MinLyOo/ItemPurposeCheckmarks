using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using System.Collections.Generic;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on MoreCheckmarks (GPLv3, TommySoucy) hideout material logic,
    // adapted to SPT 4.1's HideoutRepresentation API.
    internal static class HideoutHelper
    {
        /// <summary>
        /// Result of checking whether an item is needed for any hideout upgrade.
        /// Each area keeps its own requirement; totals are aggregated for the summary line.
        /// </summary>
        public class HideoutNeed
        {
            public bool FoundNeeded;      // Materials are still missing (any area)
            public bool FoundFulfilled;   // Already have enough for (at least) one upgrade
            public int TotalPossessed;    // Global possession of this item
            public int TotalRequired;     // Sum of every area's requirement
            public readonly List<HideoutAreaEntry> Areas = [];
        }

        public class HideoutAreaEntry(string areaName, int level, bool fulfilled)
        {
            public string AreaName = areaName;
            public int Level = level;             // The upgrade level this entry refers to
            public bool Fulfilled = fulfilled;
            public int PossessedCount;
            public int RequiredCount;
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

                // Accumulate this area's requirement separately from others.
                int areaRequired = 0;
                int areaPossessed = 0;
                bool areaAllFulfilled = true;

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

                        // Each area reports against the item's global possession count.
                        areaRequired += itemRequirement.IntCount;
                        areaPossessed = itemRequirement.UserItemsCount;

                        if (!itemRequirement.Fulfilled)
                        {
                            areaAllFulfilled = false;
                        }
                    }
                }

                if (areaRequired <= 0)
                {
                    continue;
                }

                result.Areas.Add(new HideoutAreaEntry(areaName, futureStages[0].Level, areaAllFulfilled)
                {
                    PossessedCount = areaPossessed,
                    RequiredCount = areaRequired,
                });

                result.TotalRequired += areaRequired;
                result.TotalPossessed = areaPossessed;

                if (!areaAllFulfilled)
                {
                    result.FoundNeeded = true;
                }
            }

            // If at least one area uses this item and none is missing materials,
            // it is "ready" for the next upgrade (lower priority than a need).
            if (result.Areas.Count > 0 && !result.FoundNeeded)
            {
                result.FoundFulfilled = true;
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
