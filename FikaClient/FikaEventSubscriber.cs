using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using ItemPurposeCheckmarks.Helpers;
using System.Collections.Generic;

namespace ItemPurposeCheckmarks.Fika.Helpers
{
    // Subscribes to Fika events and forwards them to the main client assembly.
    // The stash snapshot must be taken on FikaGameCreatedEvent (Fika raids do not
    // go through LocalGame.Create), otherwise the in-raid "in stash" count reads
    // an empty/stale cache and shows zero.
    // Based on AllQuestsCheckmarksFika by ZGFueDkx (GPL-3.0).
    internal static class FikaEventSubscriber
    {
        public static void Init()
        {
            FikaEventDispatcher.SubscribeEvent((FikaRaidStartedEvent e) =>
            {
                Plugin.LogDebug("Fika Raid Started");

                if (!CoopHandler.TryGetCoopHandler(out CoopHandler coopHandler))
                {
                    Plugin.LogSource?.LogError("Failed to get Fika CoopHandler");
                    return;
                }

                Dictionary<string, string> players = new Dictionary<string, string>();

                foreach (FikaPlayer player in coopHandler.HumanPlayers)
                {
                    if (player != coopHandler.MyPlayer)
                    {
                        players.Add(player.ProfileId, player.Profile.Nickname);
                    }
                }

                if (players.Count == 0)
                {
                    return;
                }

                FikaBridge.InvokeCoopPlayersEvent(players);
            });

            FikaEventDispatcher.SubscribeEvent((FikaGameEndedEvent e) =>
            {
                Plugin.LogDebug("Fika Game Ended");
                FikaBridge.InvokeRaidFinishedEvent();
            });

            FikaEventDispatcher.SubscribeEvent((FikaGameCreatedEvent e) =>
            {
                Plugin.LogDebug("Fika Game Created");
                FikaBridge.InvokeBuildCacheEvent();
            });
        }
    }
}
