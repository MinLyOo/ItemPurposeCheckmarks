using System;
using System.Collections.Generic;
using System.Reflection;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0) - loosely-coupled Fika integration
    public static class FikaBridge
    {
        public static event Action? RaidFinishedEvent;
        public static event Action? BuildCacheEvent;
        public static event Action<Dictionary<string, string>>? CoopPlayersEvent;

        public static void Init()
        {
            if (!TryInitFika())
            {
                return;
            }

            RaidFinishedEvent += SquadQuests.ClearSquadQuests;
            BuildCacheEvent += StashHelper.BuildItemsCache;
            CoopPlayersEvent += SquadQuests.LoadData;
        }

        public static void InvokeRaidFinishedEvent()
        {
            Plugin.LogDebug("InvokeRaidFinishedEvent");
            RaidFinishedEvent?.Invoke();
        }

        public static void InvokeCoopPlayersEvent(Dictionary<string, string> players)
        {
            Plugin.LogDebug("InvokeCoopPlayersEvent");
            CoopPlayersEvent?.Invoke(players);
        }

        public static void InvokeBuildCacheEvent()
        {
            Plugin.LogDebug("InvokeBuildCacheEvent");
            BuildCacheEvent?.Invoke();
        }

        private static bool TryInitFika()
        {
            try
            {
                Assembly assembly = Assembly.Load("ItemPurposeCheckmarks-Fika");
                Type main = assembly.GetType("ItemPurposeCheckmarks.Fika.Main");
                MethodInfo init = main.GetMethod("Init");

                init.Invoke(main, null);
            }
            catch (Exception e)
            {
                Plugin.LogSource?.LogError($"Failed to load ItemPurposeCheckmarks-Fika.dll! : {e}");
                return false;
            }

            return true;
        }
    }
}
