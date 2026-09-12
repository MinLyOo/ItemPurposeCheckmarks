using ItemPurposeCheckmarks.Helpers;
using ItemPurposeCheckmarks.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.IO;
using System.Reflection;
using UnityEngine;
using ZGFueDkx.ZGCLib.helpers;

namespace ItemPurposeCheckmarks
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (https://github.com/danx91/AllQuestsCheckmarks) - GPL-3.0
    [
        BepInPlugin("com.kee.itempurposecheckmarks", "ItemPurposeCheckmarks", "1.1.0"),
        BepInDependency("com.SPT.core", "4.1.0"),
        BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency),
    ]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource? LogSource;
        public static bool isFikaInstalled = false;
        public static string? modPath;

        public void Awake()
        {
            LogSource = Logger;

            modPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Plugin)).Location);
            modPath = modPath?.Replace("\\", "/");

            isFikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");

            Settings.Init(Config);
            Helpers.Assets.LoadAssets();

            // The "-Fika" bridge assembly is optional and only adds coop/squad events.
            if (isFikaInstalled && File.Exists(Path.Combine(modPath, "ItemPurposeCheckmarks-Fika.dll")))
            {
                FikaBridge.Init();
            }

            // Always snapshot the stash before a raid starts, regardless of Fika being
            // installed - otherwise the in-raid "in stash" count stays zero when the
            // raid is created through LocalGame.Create.
            new LocalGameStartPatch().Enable();

            new QuestClassPatch().Enable();
            new ProfileSelectionPatch().Enable();
            new QuestItemViewPanelPatch().Enable();
            new ItemSpecificationPanelPatch().Enable();
            new UpdateApplicationLanguagePatch().Enable();
            new TakeActionPatch().Enable();

            LogSource.LogInfo($"ItemPurposeCheckmarks version {Info.Metadata.Version} started");
        }

        public static void LogDebug(string msg)
        {
            if (Settings.ShowDebug!.Value)
            {
                LogSource?.LogDebug(msg);
            }
        }

        // Polls (only while outside a raid) so the stash cache + flea price warmup
        // starts right after the profile loads in the main menu - before the player
        // opens the stash/search screen and hovers anything.
        private float _warmCheckTimer = 1f;

        private void Update()
        {
            if (RaidUtils.IsInRaid() || StashHelper.IsCacheReady)
            {
                return;
            }

            _warmCheckTimer -= Time.unscaledDeltaTime;
            if (_warmCheckTimer > 0f)
            {
                return;
            }

            _warmCheckTimer = 2f;
            StashHelper.TryWarmInMenu();
        }
    }
}
