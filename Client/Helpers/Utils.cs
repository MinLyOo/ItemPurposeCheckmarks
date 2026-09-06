using EFT;
using System;
using System.Linq;

namespace ItemPurposeCheckmarks.Helpers
{
    internal static class Utils
    {
        public static bool IsValidMongoID(string s)
        {
            return s is { Length: 24 } && s.All(c => Uri.IsHexDigit(c));
        }

        /// <summary>
        /// Resolves the localized display name of an item template id.
        /// Item display names live under the localization key "{templateId} Name"
        /// (EFT standard, same as MoreCheckmarks). This works without accessing the
        /// runtime ItemFactory instance, which is not a Unity Singleton in SPT 4.1.
        /// </summary>
        public static string GetItemName(MongoID templateId)
        {
            try
            {
                string key = templateId.ToString() + " Name";
                string localized = key.Localized(null);

                // Localized() returns the key itself when no translation exists.
                if (!string.IsNullOrEmpty(localized) && localized != key)
                {
                    return localized;
                }
            }
            catch
            {
                // Fall through to the raw id below.
            }

            return templateId.ToString();
        }
    }
}
