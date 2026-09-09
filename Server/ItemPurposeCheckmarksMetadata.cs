using SPTarkov.Server.Core.Models.Spt.Mod;

namespace ItemPurposeCheckmarks
{
    // Server mod metadata (SPT 4.1 replacement for the legacy package.json).
    public record ItemPurposeCheckmarksMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "com.kee.itempurposecheckmarks";
        public string Name { get; init; } = "ItemPurposeCheckmarks";
        public string Author { get; init; } = "kee";
        public List<string>? Contributors { get; init; } = ["ZGFueDkx (AllQuestsCheckmarks, GPL-3.0)", "TommySoucy (MoreCheckmarks, GPLv3)"];
        public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
        public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
        public List<string>? Incompatibilities { get; init; } =
        [
            // Same UI hook as AllQuestsCheckmarks - the two mods cannot coexist.
            "com.zgfuedkx.allquestscheckmarks"
        ];
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public string? Url { get; init; }
        public string License { get; init; } = "GNU GPLv3";
        public bool HasPrepatcher { get; init; } = false;
    }
}
