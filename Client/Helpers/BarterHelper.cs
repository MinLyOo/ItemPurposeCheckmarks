using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System.Collections.Generic;
using ZGFueDkx.ZGCLib.Helpers;

namespace ItemPurposeCheckmarks.Helpers
{
    // Based on MoreCheckmarks (GPLv3, TommySoucy) barter parsing.
    // The client receives raw trader assorts JSON from the server and builds a
    // "barter currency -> offered items" index. Cash currencies are skipped.
    internal static class BarterHelper
    {
        private static readonly MongoID[] MoneyIds =
        [
            "5449016a4bdc2d6f028b456f", // Roubles
            "5696686a4bdc2da3298b456a", // Dollars
            "569668774bdc2da2298b4568", // Euros
        ];

        // Indexed by trader index -> barter currency template id -> list of (offered item id, count)
        public static List<Dictionary<MongoID, List<KeyValuePair<MongoID, int>>>> BartersByItemByTrader = [];

        public static string[] TraderNames = [];

        public static void LoadData()
        {
            BartersByItemByTrader.Clear();

            string[] names;
            try
            {
                string traderNamesResponse = RequestHandler.GetJson("/item-purpose-checkmarks/trader-names");
                if (!string.IsNullOrEmpty(traderNamesResponse) && traderNamesResponse != "null")
                {
                    JArray namesArr = JArray.Parse(traderNamesResponse);
                    names = new string[namesArr.Count];
                    for (int i = 0; i < namesArr.Count; ++i)
                    {
                        names[i] = namesArr[i]?.ToString() ?? $"Trader {i}";
                    }
                }
                else
                {
                    names = [];
                }
            }
            catch
            {
                names = [];
            }

            TraderNames = names;

            try
            {
                JArray assortData = JArray.Parse(RequestHandler.GetJson("/item-purpose-checkmarks/assorts"));

                for (int i = 0; i < assortData.Count; ++i)
                {
                    Dictionary<MongoID, List<KeyValuePair<MongoID, int>>> traderBarters = [];
                    BartersByItemByTrader.Add(traderBarters);

                    JArray? items = assortData[i]?["items"] as JArray;
                    if (items is null)
                    {
                        continue;
                    }

                    JToken? barterScheme = assortData[i]?["barter_scheme"];

                    for (int j = 0; j < items.Count; ++j)
                    {
                        JToken item = items[j];
                        // Only hideout "barter" offers are trades; money purchases are skipped.
                        if (item["parentId"]?.ToString() != "hideout")
                        {
                            continue;
                        }

                        // The barter_scheme dict is keyed by the offer item's unique _id,
                        // while the _tpl holds the actual item template id.
                        string? offerKey = item["_id"]?.ToString();
                        if (string.IsNullOrEmpty(offerKey))
                        {
                            continue;
                        }

                        string? offeredTplRaw = item["_tpl"]?.ToString();
                        if (string.IsNullOrEmpty(offeredTplRaw) || !Utils.IsValidMongoID(offeredTplRaw))
                        {
                            continue;
                        }

                        MongoID offeredTpl = offeredTplRaw;

                        JArray? barters = barterScheme?[offerKey] as JArray;
                        if (barters is null)
                        {
                            continue;
                        }

                        foreach (JToken barter in barters)
                        {
                            if (barter is not JArray priceList)
                            {
                                continue;
                            }

                            foreach (JToken price in priceList)
                            {
                                string? priceTpl = price["_tpl"]?.ToString();
                                if (string.IsNullOrEmpty(priceTpl))
                                {
                                    continue;
                                }

                                MongoID priceId = priceTpl;
                                // Skip pure money purchases - only real barter currencies count.
                                if (IsMoney(priceId))
                                {
                                    continue;
                                }

                                int count = (int)(price["count"] ?? 0);
                                if (!traderBarters.TryGetValue(priceId, out List<KeyValuePair<MongoID, int>>? offers))
                                {
                                    offers = [];
                                    traderBarters.Add(priceId, offers);
                                }

                                // Same trader can list the same exchange at several loyalty
                                // levels - keep only the cheapest required count so the
                                // tooltip does not repeat identical lines.
                                int index = offers.FindIndex(kv => kv.Key == offeredTpl);
                                if (index >= 0)
                                {
                                    if (count < offers[index].Value)
                                    {
                                        offers[index] = new KeyValuePair<MongoID, int>(offeredTpl, count);
                                    }
                                }
                                else
                                {
                                    offers.Add(new KeyValuePair<MongoID, int>(offeredTpl, count));
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                Plugin.LogSource?.LogError($"Failed to parse assort data: {ex.Message}. Barter checkmarks will be unavailable.");
            }
        }

        /// <summary>
        /// Returns, per trader, the list of (offered item template id, count) the given item
        /// can be bartered for. Empty if not used as a barter currency anywhere.
        /// </summary>
        public static List<List<KeyValuePair<MongoID, int>>> GetBarters(MongoID itemId)
        {
            List<List<KeyValuePair<MongoID, int>>> result = [];

            for (int i = 0; i < BartersByItemByTrader.Count; ++i)
            {
                if (BartersByItemByTrader[i].TryGetValue(itemId, out List<KeyValuePair<MongoID, int>>? barters))
                {
                    result.Add(barters);
                }
                else
                {
                    result.Add([]);
                }
            }

            return result;
        }

        public static bool HasAnyBarter(MongoID itemId)
        {
            for (int i = 0; i < BartersByItemByTrader.Count; ++i)
            {
                if (BartersByItemByTrader[i].ContainsKey(itemId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMoney(MongoID id)
        {
            for (int i = 0; i < MoneyIds.Length; ++i)
            {
                if (MoneyIds[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
