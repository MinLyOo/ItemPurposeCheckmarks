using ItemPurposeCheckmarks.Helpers;
using ItemPurposeCheckmarks.Patches;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using System.IO;
using System.Reflection;

namespace ItemPurposeCheckmarks
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (https://github.com/danx91/AllQuestsCheckmarks) - GPL-3.0
    [
        BepInPlugin("com.kee.itempurposecheckmarks", "ItemPurposeCheckmarks", "1.0.0"),
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

            if (isFikaInstalled)
            {
                FikaBridge.Init();
            }
            else
            {
                new LocalGameStartPatch().Enable();
            }

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
    }
}
