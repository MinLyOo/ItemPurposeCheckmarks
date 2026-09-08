using EFT;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using ZGFueDkx.ZGCLib.Config;

namespace ItemPurposeCheckmarks.Helpers
{
    // Flea market reference price (low / average / high) for the tooltip.
    // Values come from the server route which mirrors SPT's own flea price
    // calculation (RagfairController.GetItemMinAvgMaxFleaPriceValues).
    //
    // All network calls happen on a single background worker thread so the UI
    // thread never blocks. The tooltip only reads the in-memory cache; when an
    // item has not been fetched yet it is queued and the price shows from the
    // next time that item's tooltip is opened. Stash contents are prefetched in
    // bulk once StashHelper builds its cache, which covers the common case of
    // browsing/searching the stash.
    internal static class PriceHelper
    {
        private sealed class ItemPrice
        {
            public double Min;
            public double? Avg;
            public double Max;
        }

        private static readonly object CacheLock = new();
        private static readonly Dictionary<MongoID, ItemPrice?> PriceCache = [];

        // Templates already queued or fetched this session (no retries).
        private static readonly HashSet<MongoID> Requested = [];

        // Templates waiting for the background worker to fetch.
        private static readonly Queue<MongoID> Pending = [];

        private static bool _workerActive;

        /// <summary>Resolves the configured tier and returns a colorized "<number> ₽" line, or null when unavailable.</summary>
        public static string? GetPriceLine(MongoID templateId)
        {
            if (!Settings.ShowReferencePrice!.Value)
            {
                return null;
            }

            double? value = GetCachedOrQueue(templateId);
            if (value is null || value.Value <= 0)
            {
                return null;
            }

            string color = Settings.ReferencePriceColor!.GetHexColor();
            string formatted = Math.Round(value.Value).ToString("N0", CultureInfo.InvariantCulture);
            return $"<color={color}>{formatted}</color> ₽";
        }

        /// <summary>Queues a bulk prefetch for the given templates (e.g. the whole stash). Never blocks.</summary>
        public static void QueuePrefetch(IEnumerable<MongoID> templateIds)
        {
            lock (CacheLock)
            {
                foreach (MongoID templateId in templateIds)
                {
                    if (PriceCache.ContainsKey(templateId) || !Requested.Add(templateId))
                    {
                        continue;
                    }

                    Pending.Enqueue(templateId);
                }

                EnsureWorker();
            }
        }

        private static double? GetCachedOrQueue(MongoID templateId)
        {
            lock (CacheLock)
            {
                if (PriceCache.TryGetValue(templateId, out ItemPrice? cached))
                {
                    return Pick(cached);
                }

                // First sighting this session - fetch it in the background.
                if (Requested.Add(templateId))
                {
                    Pending.Enqueue(templateId);
                }

                EnsureWorker();
                return null;
            }
        }

        private static void EnsureWorker()
        {
            if (_workerActive)
            {
                return;
            }

            _workerActive = true;
            Thread worker = new(WorkerLoop)
            {
                IsBackground = true,
                Name = "IPCM-PriceFetch",
            };
            worker.Start();
        }

        private static void WorkerLoop()
        {
            while (true)
            {
                MongoID templateId;
                lock (CacheLock)
                {
                    if (Pending.Count == 0)
                    {
                        _workerActive = false;
                        return;
                    }

                    templateId = Pending.Dequeue();
                }

                // Network call happens outside the lock so one slow request does
                // not block the UI thread or other queued lookups.
                ItemPrice? price = Fetch(templateId);

                lock (CacheLock)
                {
                    PriceCache[templateId] = price;
                }
            }
        }

        private static ItemPrice? Fetch(MongoID templateId)
        {
            try
            {
                string body = JsonConvert.SerializeObject(new { templateId = templateId.ToString() });
                string response = RequestHandler.PostJson("/item-purpose-checkmarks/price", body);
                if (string.IsNullOrEmpty(response) || response == "null")
                {
                    return null;
                }

                JObject data = JObject.Parse(response);
                return new ItemPrice
                {
                    Min = data.Value<double?>("min") ?? 0,
                    Avg = data.Value<double?>("avg"),
                    Max = data.Value<double?>("max") ?? 0,
                };
            }
            catch (Exception ex)
            {
                Plugin.LogDebug($"Failed to fetch flea price for {templateId}: {ex.Message}");
                return null;
            }
        }

        private static double? Pick(ItemPrice? price)
        {
            if (price is null)
            {
                return null;
            }

            switch (Settings.ReferencePriceMode!.Value)
            {
                case "最低价":
                    return price.Min > 0 ? price.Min : null;
                case "最高价":
                    return price.Max > 0 ? price.Max : null;
                default:
                    return price.Avg ?? (price.Min > 0 ? price.Min : null);
            }
        }
    }
}
