using System.Text.Json;
using System.Text.Json.Serialization;

namespace ItemPurposeCheckmarks
{
    // User-editable server configuration, stored as config.json next to the mod DLL.
    // Ported from MoreCheckmarks (GPLv3, TommySoucy) - quest exclusion configuration.
    public class ServerConfig
    {
        [JsonPropertyName("hideInactiveEventQuests")]
        public bool HideInactiveEventQuests { get; set; } = true;

        [JsonPropertyName("excludedQuestIds")]
        public List<string> ExcludedQuestIds { get; set; } = new();

        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        /// <summary>
        /// Loads config.json from the given folder. Creates it with defaults if missing or unreadable.
        /// Never throws; returns defaults on any error.
        /// </summary>
        public static ServerConfig LoadOrCreate(string modFolder, Action<string>? logError = null)
        {
            string path = Path.Combine(modFolder, "config.json");
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    ServerConfig? parsed = JsonSerializer.Deserialize<ServerConfig>(json);
                    if (parsed is not null)
                    {
                        parsed.ExcludedQuestIds ??= new List<string>();
                        return parsed;
                    }
                }
            }
            catch (Exception ex)
            {
                logError?.Invoke($"Failed to read config.json, using defaults: {ex.Message}");
            }

            ServerConfig fresh = new();
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(fresh, WriteOptions));
            }
            catch (Exception ex)
            {
                logError?.Invoke($"Failed to write default config.json: {ex.Message}");
            }

            return fresh;
        }
    }
}
