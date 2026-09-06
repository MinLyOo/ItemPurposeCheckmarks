using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using SPT.Reflection.Utils;
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

        public class ItemsCount
        {
            public int Fir;
            public int NonFir;
            public int Total => Fir + NonFir;
        }

        public static ItemsCount GetItemsInStash(MongoID itemId)
        {
            ItemsCount itemsCount = new();
            Profile profile = ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
            IEnumerable<Item> items;

            if (RaidUtils.IsInRaid())
            {
                if (_itemsCache != null && _itemsCache.TryGetValue(itemId, out ItemsCount cached))
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

        /// <summary>
        /// Counts how many of the given item are currently on the player's character
        /// (equipment + in-raid pockets/backpack), i.e. "on you" rather than in stash.
        /// </summary>
        public static int GetOnYouCount(MongoID itemId)
        {
            int count = 0;

            try
            {
                Profile profile = ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
                IEnumerable<Item> items = profile.Inventory.GetPlayerItems(EPlayerItems.Equipment | EPlayerItems.QuestItems)
                    .Where(i => i.TemplateId == itemId);

                foreach (Item item in items)
                {
                    count += item.StackObjectsCount;
                }
            }
            catch
            {
                // Profile may not be ready; just report zero.
            }

            return count;
        }

        public static void BuildItemsCache()
        {
            _itemsCache.Clear();

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
        }
    }
}
