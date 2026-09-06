using BepInEx.Logging;
using System;
using System.Threading.Tasks;

namespace ZGFueDkx.ZGCLib.Helpers
{
    // Based on ZGFueDkx's Common Library (https://github.com/danx91/SPT-ZGFueDkxCommonLibrary) - GPL-3.0
    internal static class TaskUtils
    {
        public static void FireAndForget(this Task task, ManualLogSource? logSource, Action<Exception>? onError = null)
        {
            task.ContinueWith(t =>
            {
                var ex = t.Exception?.Flatten();
                if (ex is not null)
                {
                    (onError ?? (e => logSource?.LogError(e.ToString())))(ex);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
