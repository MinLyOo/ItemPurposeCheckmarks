using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using SPT.Reflection.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using ZGFueDkx.ZGCLib.helpers;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0)
    class StashHelper
    {
        public static readonly List<MongoID> MoneyIds =
        [
            "5449016a4bdc2d6f028b456f", //Roubles
            "5696686a4bdc2da3298b456a", //Dollars
            "569668774bdc2da2298b4568", //Euros
        ];

        private static readonly Dictionary<MongoID, ItemsCount> _itemsCache = [];

        // Guards the in-raid one-shot rebuild so a failed attempt (stash not reachable
        // mid-raid) is not retried on every tooltip.
        private static bool _raidRebuildAttempted;

        // Guards the main-menu poll so it attempts the early build only once.
        private static bool _earlyWarmAttempted;

        public class ItemsCount
        {
            public int Fir;
            public int NonFir;
            public int Total => Fir + NonFir;
        }

        private static DateTime _lastStashLogTime = DateTime.MinValue;

        public static ItemsCount GetItemsInStash(MongoID itemId)
        {
            ItemsCount itemsCount = new();
            var session = ClientAppUtils.GetClientApp()?.GetClientBackEndSession();
            if (session?.Profile is null)
            {
                return itemsCount;
            }

            Profile profile = session.Profile;
            IEnumerable<Item> items;

            // Diagnostics for the in-raid "stash shows 0" bug (only with debug logging on).
            // Rate-limited to at most once every 5 seconds so browsing the stash doesn't
            // flood the log.
            if (Settings.ShowDebug!.Value)
            {
                DateTime now = DateTime.UtcNow;
                if ((now - _lastStashLogTime).TotalSeconds >= 5)
                {
                    _lastStashLogTime = now;
                    Plugin.LogDebug($"StashCount item={itemId} inRaid={RaidUtils.IsInRaid()} cacheSize={_itemsCache.Count}");
                }
            }

            if (RaidUtils.IsInRaid())
            {
                // When the pre-raid snapshot is missing entirely (e.g. the raid never went
                // through LocalGame.Create) try to rebuild once from reachable stash data.
                if (_itemsCache.Count == 0)
                {
                    TryRebuildCache();
                }

                if (_itemsCache.TryGetValue(itemId, out ItemsCount cached))
                {
                    itemsCount.Fir += cached.Fir;
                    itemsCount.NonFir += cached.NonFir;
                }

                if (!Settings.IncludeRaidItems!.Value)
                {
                    return itemsCount;
                }

                items = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment | EPlayerItems.QuestItems).Where(i => i.TemplateId == itemId);
            }
            else
            {
                // Outside a raid the full profile (stash included) is always reachable.
                // Keep a snapshot so a later raid still shows the pre-raid stash counts
                // even if the stash becomes unreachable once the raid profile loads.
                if (_itemsCache.Count == 0)
                {
                    BuildItemsCache();
                }

                items = profile.Inventory.GetPlayerItems().Where(i => i.TemplateId == itemId);
            }

            foreach (Item item in items)
            {
                if (item.MarkedAsSpawnedInSession)
                {
                    itemsCount.Fir += item.StackObjectsCount;
                }
                else
                {
                    itemsCount.NonFir += item.StackObjectsCount;
                }
            }

            return itemsCount;
        }

        private static void TryRebuildCache()
        {
            if (_raidRebuildAttempted)
            {
                return;
            }

            _raidRebuildAttempted = true;

            BuildItemsCache();

            // Successfully rebuilt - allow another attempt for a future raid.
            if (_itemsCache.Count > 0)
            {
                _raidRebuildAttempted = false;
            }
        }

        /// <summary>
        /// Counts how many of the given item are currently on the player's character
        /// (equipment + in-raid pockets/backpack), i.e. "on you" rather than in stash.
        /// Returns 0 when the profile is not yet ready (first frame after menu).
        /// </summary>
        public static int GetOnYouCount(MongoID itemId)
        {
            int count = 0;

            try
            {
                var session = ClientAppUtils.GetClientApp()?.GetClientBackEndSession();
                if (session?.Profile is null)
                {
                    return 0;
                }

                IEnumerable<Item> items = session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment | EPlayerItems.QuestItems)
                    .Where(i => i.TemplateId == itemId);

                foreach (Item item in items)
                {
                    count += item.StackObjectsCount;
                }
            }
            catch (Exception ex)
            {
                Plugin.LogDebug($"GetOnYouCount failed: {ex.GetType().Name}: {ex.Message}");
            }

            return count;
        }

        public static bool IsCacheReady => _itemsCache.Count > 0;

        /// <summary>
        /// Called periodically from Plugin.Update while outside a raid. Once the
        /// profile is available (main menu) this builds the stash cache and warms
        /// the flea prices for the whole stash - long before the player opens the
        /// stash/search screen, so the first tooltip already shows the price.
        /// </summary>
        public static void TryWarmInMenu()
        {
            if (_earlyWarmAttempted || _itemsCache.Count > 0)
            {
                return;
            }

            try
            {
                if (RaidUtils.IsInRaid())
                {
                    return;
                }

                // Profile not loaded yet (e.g. still on the profile selection
                // screen) - silently retry on a later tick.
                var session = ClientAppUtils.GetClientApp()?.GetClientBackEndSession();
                if (session?.Profile is null)
                {
                    return;
                }

                _earlyWarmAttempted = true;
                BuildItemsCache();
            }
            catch
            {
                // Not ready yet - retry on a later tick.
            }
        }

        public static void BuildItemsCache()
        {
            _itemsCache.Clear();

            try
            {
                Profile profile = ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
                IEnumerable<Item> itemsToCache = profile.Inventory.GetPlayerItems(EPlayerItems.HideoutStashes);
                IEnumerable<Item>? stashItems = Singleton<HideoutRepresentation>.Instance?.AllStashItems;

                if (stashItems != null)
                {
                    itemsToCache = itemsToCache.Concat(stashItems);
                }

                foreach (Item item in itemsToCache)
                {
                    if (_itemsCache.TryGetValue(item.TemplateId, out ItemsCount itemsCount))
                    {
                        if (item.MarkedAsSpawnedInSession)
                        {
                            itemsCount.Fir += item.StackObjectsCount;
                        }
                        else
                        {
                            itemsCount.NonFir += item.StackObjectsCount;
                        }
                    }
                    else
                    {
                        ItemsCount count = new();

                        if (item.MarkedAsSpawnedInSession)
                        {
                            count.Fir = item.StackObjectsCount;
                        }
                        else
                        {
                            count.NonFir = item.StackObjectsCount;
                        }

                        _itemsCache.Add(item.TemplateId, count);
                    }
                }

                Plugin.LogSource?.LogInfo($"Items cache built. Total items in cache: {_itemsCache.Count}");

                // Warm the flea reference prices for everything we found in the
                // stash so tooltips never trigger a blocking request later.
                PriceHelper.QueuePrefetch(_itemsCache.Keys);
            }
            catch (System.Exception ex)
            {
                Plugin.LogSource?.LogError($"Failed to build items cache: {ex.Message}");
            }

            if (_itemsCache.Count == 0)
            {
                Plugin.LogSource?.LogWarning("Items cache is empty - stash items may not be reachable right now.");
            }
        }
    }
}
