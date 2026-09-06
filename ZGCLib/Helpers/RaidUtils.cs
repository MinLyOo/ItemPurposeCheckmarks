using Comfort.Common;
using EFT;

namespace ZGFueDkx.ZGCLib.helpers
{
    // Based on ZGFueDkx's Common Library (https://github.com/danx91/SPT-ZGFueDkxCommonLibrary) - GPL-3.0
    internal static class RaidUtils
    {
        public static bool IsInRaid()
        {
            return Singleton<AbstractGame>.Instance?.InRaid is true;
        }
    }
}
