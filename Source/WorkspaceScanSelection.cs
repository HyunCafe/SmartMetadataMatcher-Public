using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Pure selection policy. This authorizes read-only IGDB lookups only;
    // marks are never carried into identity verification or import approval.
    internal static class WorkspaceScanSelection
    {
        internal const int BatchLimit = 25;

        internal static List<Guid> Choose(IEnumerable<Guid> pending,
            IEnumerable<Guid> marked, int limit = BatchLimit)
        {
            if (pending == null || marked == null)
                throw new ArgumentNullException(pending == null ? "pending" : "marked");
            if (limit < 1 || limit > BatchLimit)
                throw new ArgumentOutOfRangeException("limit");
            var marks = new HashSet<Guid>(marked.Where(x => x != Guid.Empty));
            var seen = new HashSet<Guid>();
            return pending.Where(id => id != Guid.Empty && seen.Add(id) &&
                    (marks.Count == 0 || marks.Contains(id)))
                .Take(limit).ToList();
        }
    }
}
