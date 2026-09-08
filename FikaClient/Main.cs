using ItemPurposeCheckmarks.Fika.Helpers;

namespace ItemPurposeCheckmarks.Fika
{
    // Loaded reflectively by ItemPurposeCheckmarks.Helpers.FikaBridge when the
    // client plugin detects Fika. Kept in a separate assembly so the main client
    // never hard-references Fika.Core.
    // Based on AllQuestsCheckmarksFika by ZGFueDkx (GPL-3.0).
    internal static class Main
    {
        public static void Init()
        {
            FikaEventSubscriber.Init();
            Plugin.LogSource?.LogInfo("ItemPurposeCheckmarks Fika module loaded");
        }
    }
}
