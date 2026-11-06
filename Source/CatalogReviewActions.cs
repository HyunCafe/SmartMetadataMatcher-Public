using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Pure checks used by the in-plugin IGDB search. Catalog discovery never
    // confers permission to write metadata without explicit identity approval.
    internal static class CatalogReviewActions
    {
        internal static bool PlatformVerified(string playnitePlatform,long catalogId,
            string catalogTitle,IEnumerable<long> catalogPlatformIds)
        {
            long expected=CatalogCore.PlatformId(playnitePlatform);
            return expected>0 && catalogId>0 &&
                !String.IsNullOrWhiteSpace(catalogTitle) && catalogPlatformIds!=null &&
                catalogPlatformIds.Contains(expected);
        }
        internal static bool SameLiveIdentity(Guid gameId,string savedTitle,string savedPlatform,
            Guid liveId,string liveTitle,string livePlatform)
        {
            return gameId!=Guid.Empty && liveId==gameId &&
                String.Equals(savedTitle,liveTitle,StringComparison.Ordinal) &&
                String.Equals(savedPlatform,livePlatform,StringComparison.Ordinal);
        }
    }
}
