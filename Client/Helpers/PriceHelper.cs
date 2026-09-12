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

        // Rate-limit: at most one batch price request per 30 seconds.
        // Prevents flooding the server console with "[客户端请求]" logs even when
        // the player rapidly browses different stash containers or item screens.
        private static DateTime _lastBatchTime = DateTime.MinValue;
        private static readonly TimeSpan _batchMinInterval = TimeSpan.FromSeconds(30);

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

            // Rate-limit: don't start a new worker within 30s of the last batch
            // so rapid stash browsing doesn't produce a server log line each time.
            if (DateTime.UtcNow - _lastBatchTime < _batchMinInterval)
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
                List<MongoID> batch;
                lock (CacheLock)
                {
                    if (Pending.Count == 0)
                    {
                        _workerActive = false;
                        return;
                    }

                    batch = [.. Pending];
                    Pending.Clear();
                }

                // Rate-limit guard inside the loop too, in case the worker was
                // started before _lastBatchTime was updated by a concurrent batch.
                TimeSpan elapsed = DateTime.UtcNow - _lastBatchTime;
                if (elapsed < _batchMinInterval)
                {
                    // Too soon – re-queue items and wait.
                    int sleepMs = (int)(_batchMinInterval - elapsed).TotalMilliseconds + 100;
                    Thread.Sleep(sleepMs);
                    lock (CacheLock)
                    {
                        foreach (MongoID id in batch)
                        {
                            if (!PriceCache.ContainsKey(id))
                            {
                                Pending.Enqueue(id);
                            }
                        }
                    }
                    continue;
                }

                // Single batch network call outside the lock so the UI thread
                // never blocks. All results are cached at once, replacing the
                // previous one-by-one request pattern that flooded the server log.
                _lastBatchTime = DateTime.UtcNow;
                BatchFetch(batch);
            }
        }

        private static void BatchFetch(List<MongoID> templateIds)
        {
            try
            {
                List<string> ids = [];
                foreach (MongoID tpl in templateIds)
                {
                    ids.Add(tpl.ToString());
                }

                string body = JsonConvert.SerializeObject(ids);
                string response = RequestHandler.PostJson("/item-purpose-checkmarks/prices", body);
                if (string.IsNullOrEmpty(response) || response == "null")
                {
                    return;
                }

                JObject data = JObject.Parse(response);
                lock (CacheLock)
                {
                    foreach (MongoID tpl in templateIds)
                    {
                        JToken? entry = data[tpl.ToString()];
                        PriceCache[tpl] = entry is null
                            ? null
                            : new ItemPrice
                            {
                                Min = entry.Value<double?>("min") ?? 0,
                                Avg = entry.Value<double?>("avg"),
                                Max = entry.Value<double?>("max") ?? 0,
                            };
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LogDebug($"Failed to batch fetch flea prices: {ex.Message}");
                // Mark all as null so they are not retried.
                lock (CacheLock)
                {
                    foreach (MongoID tpl in templateIds)
                    {
                        PriceCache[tpl] = null;
                    }
                }
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
