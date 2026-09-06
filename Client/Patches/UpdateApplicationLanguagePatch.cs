using EFT;
using HarmonyLib;
using ItemPurposeCheckmarks.Helpers;
using SPT.Reflection.Patching;
using System.Reflection;

namespace ItemPurposeCheckmarks.Patches
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0)
    class UpdateApplicationLanguagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalizationManager), nameof(LocalizationManager.UpdateApplicationLanguage));
        }

        [PatchPostfix]
        static void Postfix()
        {
            Locales.LoadLocale(LocalizationManager.Instance.Culture);
        }
    }
}
