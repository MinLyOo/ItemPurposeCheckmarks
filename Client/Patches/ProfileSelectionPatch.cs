using EFT;
using HarmonyLib;
using ItemPurposeCheckmarks.Helpers;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ItemPurposeCheckmarks.Patches
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0)
    internal class ProfileSelectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EftClientBackendSession.CG_SetMainProfile), nameof(EftClientBackendSession.CG_SetMainProfile.method_0));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Plugin.LogDebug("Profile selected");
            QuestsData.LoadData();
        }
    }
}
