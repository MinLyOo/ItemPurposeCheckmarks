using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace ItemPurposeCheckmarks
{
    // Static routes consumed by the BepInEx client plugin.
    // Based on AllQuestsCheckmarks by ZGFueDkx (GPL-3.0);
    // assorts/trader-names/productions routes ported from MoreCheckmarks (MIT).
    [Injectable(TypePriority = OnLoadOrder.Routers + 1)]
    internal class ItemPurposeCheckmarksRouter(
        JsonUtil jsonUtil,
        ItemPurposeCheckmarksMod mod
    ) : StaticRouter(
        jsonUtil,
        [
            new RouteAction<EmptyRequestData>(
                "/item-purpose-checkmarks/quests",
                (url, info, sessionId, output, cancellationToken) => mod.GetAllQuests(sessionId)
            ),
            new RouteAction<ActiveQuestsRequestData>(
                "/item-purpose-checkmarks/active-quests",
                (url, info, sessionId, output, cancellationToken) => mod.HandleGetActiveQuests(info)
            ),
            new RouteAction<EmptyRequestData>(
                "/item-purpose-checkmarks/assorts",
                (url, info, sessionId, output, cancellationToken) => mod.HandleAssorts(sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/item-purpose-checkmarks/trader-names",
                (url, info, sessionId, output, cancellationToken) => mod.HandleTraderNames(sessionId)
            ),
            new RouteAction<EmptyRequestData>(
                "/item-purpose-checkmarks/productions",
                (url, info, sessionId, output, cancellationToken) => mod.HandleProductions()
            ),
        ]
    )
    {
        // One-off startup work that needs the fully constructed mod singleton.
        private readonly bool _booted = Boot(mod);

        private static bool Boot(ItemPurposeCheckmarksMod mod)
        {
            mod.WriteQuestReference();
            return true;
        }

        private class ActiveQuestsRequestData : List<MongoId>, IRequestData;
    }
}
