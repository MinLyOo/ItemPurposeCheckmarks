using EFT;
using HarmonyLib;
using ItemPurposeCheckmarks.Helpers;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ItemPurposeCheckmarks.Patches
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0)
    // Used when Fika is not installed (Fika provides its own raid-start event).
    internal class LocalGameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalGame), nameof(LocalGame.Create));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Plugin.LogDebug("Local game started");
            StashHelper.BuildItemsCache();
        }
    }
}
