using BepInEx.Configuration;
using UnityEngine;
using ZGFueDkx.ZGCLib.Config;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0)
    // Extended with MoreCheckmarks (MIT) information-dimension colors.
    // All display names are localized in Chinese; each info dimension has its own visibility toggle.
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
            ConfigCategory general = config.MakeCategory(1, "常规");
            ConfigCategory purpose = config.MakeCategory(2, "用途（显示开关）");
            ConfigCategory colors = config.MakeCategory(3, "勾选颜色");
            ConfigCategory text = config.MakeCategory(4, "文本颜色");
            ConfigCategory debug = config.MakeCategory(9, "调试");

            /*
             * 常规
             */
            IncludeCollector = general.BindConfig(
                "计入收藏家任务所需（Fence）",
                true,
                "是否计入收藏家（Collector）任务所需的物品"
            );

            IncludeNonFir = general.BindConfig(
                "计入非战局任务",
                true,
                "是否计入无需“战局中找到（FiR）”即可上交的任务"
            );

            IncludeLoyaltyRegain = general.BindConfig(
                "计入挽回声望类任务",
                false,
                "是否计入用于挽回声望的任务：Fence 补偿任务、Lightkeeper 的 Make Amends、Chemical 任务线尾声"
            );

            IncludeUnreachable = general.BindConfig(
                "计入不可达任务",
                false,
                "是否计入不可达任务（活动任务、其他账号类型的任务）"
            );

            HideFulfilled = general.BindConfig(
                "战局中已集齐则隐藏勾选",
                false,
                "战局内若某物品已满足所有进行中与未来任务所需，则隐藏其勾选。与“计入战局内物品（PMC 背包）”组合使用时需谨慎，可能在其他物品上误隐藏"
            );

            OnlyActiveQuests = general.BindConfig(
                "仅显示进行中任务",
                false,
                "是否只显示进行中任务，不显示未来任务"
            );

            IncludeRaidItems = general.BindConfig(
                "战局内物品计入库存计数",
                false,
                "是否在战局内把 PMC 背包中的物品计入 “库存中” 计数"
            );

            /*
             * 用途（显示开关）—— 每个信息维度单独控制是否显示
             */
            ShowHideout = purpose.BindConfig(
                "显示藏身处升级需要",
                true,
                "标记藏身处设施升级所需的材料。tooltip 里按设施分别显示，标题给合计"
            );

            ShowBarter = purpose.BindConfig(
                "显示商人交易（Barter）",
                true,
                "标记可作为商人非现金（Barter）交易货币的物品"
            );

            ShowCraft = purpose.BindConfig(
                "显示制造配方产物",
                true,
                "标记藏身处制造配方中的原料，tooltip 显示能造出什么"
            );

            ShowWishlist = purpose.BindConfig(
                "显示愿望单物品",
                true,
                "标记位于你愿望单中的物品"
            );

            ShowAllFutureHideoutLevels = purpose.BindConfig(
                "检查藏身处所有未来等级",
                false,
                "关闭时仅检查下一级；打开时检查所有可达的升级等级"
            );

            OnlyShowHideoutOnFir = purpose.BindConfig(
                "藏身处勾选仅显示于战局物品",
                false,
                "只对“战局中找到（FiR）”的物品显示藏身处勾选"
            );

            ShowPrereqCount = purpose.BindConfig(
                "显示任务前置数量",
                true,
                "在每个未来任务行显示还有几个前置任务未完成"
            );

            ShowOnYouCount = purpose.BindConfig(
                "显示身上持有数",
                true,
                "显示当前角色身上装有这件物品的数量"
            );

            ColorizeTakeAction = purpose.BindConfig(
                "战局捡取（Take）按钮染色",
                true,
                "按勾选颜色为战局内散货的“Take”交互按钮染色"
            );

            /*
             * 勾选颜色
             */
            CheckmarkColor = colors.BindColor(
                "未来任务勾选颜色",
                "#bf00ff",
                "物品当前不急需、但未来任务需要时的勾选颜色"
            );

            NonFirColor = colors.BindColor(
                "未来任务勾选颜色（非战局）",
                "#73264d",
                "非战局物品在仅未来任务需要时的勾选颜色"
            );

            CollectorColor = colors.BindColor(
                "收藏家任务勾选颜色",
                "#bf00ff",
                "仅为收藏家任务所需的勾选颜色"
            );

            MarkEnoughItems = colors.BindConfig(
                "已集齐时使用不同颜色",
                false,
                "是否在已满足所有任务所需时改用专门颜色。战局中可由“已集齐则隐藏勾选”隐藏"
            );

            EnoughItemsColor = colors.BindColor(
                "已集齐勾选颜色",
                "#00ff00",
                "已满足所有任务所需时的勾选颜色"
            );

            UseCustomQuestColor = colors.BindConfig(
                "进行中任务使用自定义颜色",
                false,
                "是否对进行中任务的勾选使用自定义颜色"
            );

            CustomQuestColor = colors.BindColor(
                "进行中任务自定义颜色",
                "#ffeb6d",
                "进行中任务勾选的自定义颜色"
            );

            /*
             * 用途配色（融合维度各自的勾选颜色）
             */
            HideoutColor = purpose.BindColor(
                "藏身处缺料勾选颜色",
                "#ff8c1a",
                "藏身处升级材料仍缺少时的勾选颜色"
            );

            HideoutFulfilledColor = purpose.BindColor(
                "藏身处已够勾选颜色",
                "#33cc66",
                "已凑够（至少下一级）藏身处升级材料时的勾选颜色"
            );

            WishlistColor = purpose.BindColor(
                "愿望单勾选颜色",
                "#ffd400",
                "物品在愿望单中的勾选颜色"
            );

            BarterColor = purpose.BindColor(
                "交易勾选颜色",
                "#39c0ed",
                "物品作为商人 Barter 货币时的勾选颜色"
            );

            CraftColor = purpose.BindColor(
                "制造勾选颜色",
                "#b388ff",
                "物品是制造配方原料时的勾选颜色"
            );

            /*
             * 文本颜色
             */
            BulletPoints = text.BindConfig(
                "使用项目符号",
                true,
                "任务清单是否使用 “·” 项目符号"
            );

            CustomTextColors = text.BindConfig(
                "使用自定义文本颜色",
                false,
                "是否对任务文字使用自定义颜色"
            );

            ActiveQuestTextColor = text.BindColor(
                "进行中任务文本颜色",
                "#dd831a",
                "进行中任务文字的自定义颜色"
            );

            FutureQuestTextColor = text.BindColor(
                "未来任务文本颜色",
                "#d24dff",
                "未来任务文字的自定义颜色"
            );

            if (Plugin.isFikaInstalled)
            {
                SquadQuests = general.BindConfig(
                    "标记小队成员任务",
                    true,
                    "是否标记小队成员当前所需物品"
                );

                SquadColor = colors.BindColor(
                    "小队任务勾选颜色",
                    "#ff3333",
                    "物品仅为队友所需时的勾选颜色"
                );

                SquadQuestTextColor = text.BindColor(
                    "小队任务文本颜色",
                    "#ffc299",
                    "小队任务文字的自定义颜色"
                );
            }

            /*
             * 调试
             */
            ShowDebug = debug.BindConfig(
                "调试日志",
                false,
                "在 Player.log 输出调试日志"
            );

            debug.BindButton(
                "重新加载任务数据",
                "重新加载",
                "从服务端重新加载任务数据",
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
                case "计入收藏家任务所需（Fence）":
                case "计入非战局任务":
                case "计入挽回声望类任务":
                    QuestsData.LoadData();
                    break;
            }
        }
    }
}
