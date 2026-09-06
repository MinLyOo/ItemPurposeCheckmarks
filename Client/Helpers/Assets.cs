using UnityEngine;

namespace ItemPurposeCheckmarks.Helpers
{
    internal static class Assets
    {
        public static Sprite? Checkmark;

        public static void LoadAssets()
        {
            // Bundle copied from AllQuestsCheckmarks (ZGFueDkx) - contains the "checkmark" sprite
            AssetBundle bundle = AssetBundle.LoadFromFile(Plugin.modPath + "/ItemPurposeCheckmarksAssets");

            if (bundle is null)
            {
                Plugin.LogSource?.LogError("Failed to load asset bundle!");
                return;
            }

            Checkmark = bundle.LoadAsset<Sprite>("checkmark");

            Plugin.LogSource?.LogInfo("Assets loaded");
        }
    }
}
