using System;

namespace ZGFueDkx.ZGCLib.Helpers
{
    // Based on ZGFueDkx's Common Library (https://github.com/danx91/SPT-ZGFueDkxCommonLibrary) - GPL-3.0
    internal class JsonUtils
    {
        [AttributeUsage(AttributeTargets.Property)]
        internal sealed class JsonIgnoreErrorAttribute : Attribute;
    }
}
