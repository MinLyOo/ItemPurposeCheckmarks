using BepInEx.Configuration;
using UnityEngine;
using ZGFueDkx.ZGCLib.Config;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0)
    // Extended with MoreCheckmarks (MIT) information-dimension colors.
    internal static class Settings
    {
        public static ConfigEntry<bool>? IncludeCollector;
        public static ConfigEntry<bool>? IncludeLoyaltyRegain;
        public static ConfigEntry<bool>? IncludeUnreachable;
        public static ConfigEntry<bool>? IncludeNonFir;
        public static ConfigEntry<bool>? HideFulfilled;
        public static ConfigEntry<bool>? OnlyActiveQuests;
        public static ConfigEntry<bool>? IncludeRaidItems;
        public static ConfigEntry<bool>? SquadQuests;
        public static ConfigEntry<bool>? MarkEnoughItems;
        public static ConfigEntry<bool>? UseCustomQuestColor;
        public static ConfigEntry<bool>? BulletPoints;
        public static ConfigEntry<bool>? CustomTextColors;
        public static ConfigEntry<bool>? ShowDebug;

        // --- Merged MoreCheckmarks dimension toggles ---
        public static ConfigEntry<bool>? ShowHideout;
        public static ConfigEntry<bool>? ShowBarter;
        public static ConfigEntry<bool>? ShowCraft;
        public static ConfigEntry<bool>? ShowWishlist;
        public static ConfigEntry<bool>? ShowAllFutureHideoutLevels;
        public static ConfigEntry<bool>? OnlyShowHideoutOnFir;
        public static ConfigEntry<bool>? ShowPrereqCount;
        public static ConfigEntry<bool>? ShowOnYouCount;
        public static ConfigEntry<bool>? ColorizeTakeAction;

        public static ConfigEntry<Color>? CheckmarkColor;
        public static ConfigEntry<Color>? NonFirColor;
        public static ConfigEntry<Color>? CollectorColor;
        public static ConfigEntry<Color>? EnoughItemsColor;
        public static ConfigEntry<Color>? CustomQuestColor;
        public static ConfigEntry<Color>? SquadColor;
        public static ConfigEntry<Color>? ActiveQuestTextColor;
        public static ConfigEntry<Color>? FutureQuestTextColor;
        public static ConfigEntry<Color>? SquadQuestTextColor;

        // --- Merged MoreCheckmarks dimensions (checkmark colors) ---
        public static ConfigEntry<Color>? HideoutColor;
        public static ConfigEntry<Color>? HideoutFulfilledColor;
        public static ConfigEntry<Color>? WishlistColor;
        public static ConfigEntry<Color>? BarterColor;
        public static ConfigEntry<Color>? CraftColor;

        public static void Init(ConfigFile config)
        {
            ConfigCategory general = config.MakeCategory(1, "General");
            ConfigCategory colors = config.MakeCategory(2, "Colors");
            ConfigCategory text = config.MakeCategory(3, "Text");
            ConfigCategory purpose = config.MakeCategory(4, "Purpose colors");
            ConfigCategory debug = config.MakeCategory(9, "Debug");

            /*
             * GENERAL
             */
            IncludeCollector = general.BindConfig(
                "Include Collector quest (Fence)",
                true,
                "Whether or not to include items needed for Collector quest"
            );

            IncludeNonFir = general.BindConfig(
                "Include non-FiR quests",
                true,
                "Whether or not to include quests that don't require found in raid items"
            );

            IncludeLoyaltyRegain = general.BindConfig(
                "Include loyalty regain quests",
                false,
                "Whether or not to include quests for regaining loyalty (Compensation for Damage (Fence), Make Amends (Lightkeeper) & Chemical questline finale)"
            );

            IncludeUnreachable = general.BindConfig(
                "Include unreachable quests",
                false,
                "Whether or not to include quests that are unreachable (event quests and quests for other account types)"
            );

            HideFulfilled = general.BindConfig(
                "Hide checkmark if have enough (in raid)",
                false,
                "Whether or not to hide checkmark in raid on items that you have enough for all active and future quests. Be careful when using with " +
                    "'Include items in PMC inventory (in raid)', as this combo may hide checkmarks while still in raid!"
            );

            OnlyActiveQuests = general.BindConfig(
                "Show only active quests",
                false,
                "Whether or not to show only active quests (no future quests)"
            );

            IncludeRaidItems = general.BindConfig(
                "Include items in PMC inventory (in raid)",
                false,
                "Whether or not to include items in PMC inventory while in raid in 'In Stash' count"
            );

            /*
             * PURPOSE TOGGLES (merged MoreCheckmarks dimensions)
             */
            ShowHideout = purpose.BindConfig(
                "Show hideout upgrade materials",
                true,
                "Mark items that are required materials for hideout area upgrades"
            );

            ShowBarter = purpose.BindConfig(
                "Show barter trade items",
                true,
                "Mark items used as currency in trader barter deals (non-money offers)"
            );

            ShowCraft = purpose.BindConfig(
                "Show crafting ingredients",
                true,
                "Mark items used as ingredients in hideout crafting recipes"
            );

            ShowWishlist = purpose.BindConfig(
                "Show wishlist items",
                true,
                "Mark items that are on your wishlist"
            );

            ShowAllFutureHideoutLevels = purpose.BindConfig(
                "Check all future hideout levels",
                false,
                "If off, only the next upgrade level is checked; if on, every reachable level is checked"
            );

            OnlyShowHideoutOnFir = purpose.BindConfig(
                "Hideout checkmark only on FiR items",
                false,
                "Only show the hideout checkmark on found-in-raid items"
            );

            ShowPrereqCount = purpose.BindConfig(
                "Show quest prerequisite count",
                true,
                "Show how many prerequisite quests are still unfinished for each future quest"
            );

            ShowOnYouCount = purpose.BindConfig(
                "Show On You count",
                true,
                "Show how many of this item are currently on your character (equipped/in-raid)"
            );

            ColorizeTakeAction = purpose.BindConfig(
                "Colorize the Take action on loot",
                true,
                "Tint the 'Take' interaction label of loose loot using the same color as the checkmark"
            );

            /*
             * COLORS
             */
            CheckmarkColor = colors.BindColor(
                "Checkmark color",
                "#bf00ff",
                "Color of checkmark if item is not currently needed but is required for future quests"
            );

            NonFirColor = colors.BindColor(
                "Checkmark color (non-FIR)",
                "#73264d",
                "Color of checkmark if non-FiR item is not currently needed but is required for future quests"
            );

            CollectorColor = colors.BindColor(
                "Collector color",
                "#bf00ff",
                "Color of checkmark for collector quest"
            );

            MarkEnoughItems = colors.BindConfig(
                "Use different color if have enough",
                false,
                "Whether or not to use different checkmark color if you have enough items for all quests. " +
                    "'Hide checkmark if have enough' option will hide this checkmark while in raid"
            );

            EnoughItemsColor = colors.BindColor(
                "Have enough color",
                "#00ff00",
                "Color of checkmark if you have enough items for all quests"
            );

            UseCustomQuestColor = colors.BindConfig(
                "Use custom quest checkmark color",
                false,
                "Whether or not to use custom checkmark color for active quests"
            );

            CustomQuestColor = colors.BindColor(
                "Custom quest color",
                "#ffeb6d",
                "Custom color of default quest checkmark"
            );

            /*
             * PURPOSE COLORS (merged MoreCheckmarks dimensions)
             */
            HideoutColor = purpose.BindColor(
                "Hideout need color",
                "#ff8c1a",
                "Color of checkmark if materials are still missing for a hideout upgrade"
            );

            HideoutFulfilledColor = purpose.BindColor(
                "Hideout ready color",
                "#33cc66",
                "Color of checkmark if you already have enough materials for (at least) the next hideout upgrade"
            );

            WishlistColor = purpose.BindColor(
                "Wishlist color",
                "#ffd400",
                "Color of checkmark if the item is on your wishlist"
            );

            BarterColor = purpose.BindColor(
                "Barter color",
                "#39c0ed",
                "Color of checkmark if the item is used as currency in a trader barter deal"
            );

            CraftColor = purpose.BindColor(
                "Craft color",
                "#b388ff",
                "Color of checkmark if the item is an ingredient of a hideout crafting recipe"
            );

            /*
             * TEXT
             */
            BulletPoints = text.BindConfig(
                "Use bullet points",
                true,
                "Whether or not to use bullet points in quests list"
            );

            CustomTextColors = text.BindConfig(
                "Use custom text colors",
                false,
                "Whether or not to use custom text colors"
            );

            ActiveQuestTextColor = text.BindColor(
                "Custom text color - active quests",
                "#dd831a",
                "Custom color of active quests text"
            );

            FutureQuestTextColor = text.BindColor(
                "Custom text color - future quests",
                "#d24dff",
                "Custom color of future quests text"
            );

            if (Plugin.isFikaInstalled)
            {
                SquadQuests = general.BindConfig(
                    "Mark squad members quests",
                    true,
                    "Wether or not to mark items currently needed for players in your squad"
                );

                SquadColor = colors.BindColor(
                    "Checkmark color (squad members)",
                    "#ff3333",
                    "Color of checkmark if item is not currently needed but is required for one of your squad members"
                );

                SquadQuestTextColor = text.BindColor(
                    "Custom text color - squad quests",
                    "#ffc299",
                    "Custom color of squad quests text"
                );
            }

            /*
             * DEBUG
             */
            ShowDebug = debug.BindConfig(
                "Debug logs",
                false,
                "Enable debug logs in Player.log"
            );

            debug.BindButton(
                "Reload quests data",
                "Reload",
                "Reload quests data from server",
                () =>
                {
                    QuestsData.LoadData();
                }
            );

            config.SettingChanged += SettingChanged;
            Plugin.LogSource?.LogInfo("Settings loaded");
        }

        private static void SettingChanged(object sender, SettingChangedEventArgs args)
        {
            switch (args.ChangedSetting.Definition.Key)
            {
                case "Include Collector quest (Fence)":
                case "Include non-FiR quest":
                case "Include loyalty regain quests":
                    QuestsData.LoadData();
                    break;
            }
        }
    }
}
