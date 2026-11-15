using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // A fresh lookup is only new evidence, never identity confirmation or write approval.
    // This policy limits explicit refreshes and detects changed catalog identities.
    internal static class WorkspaceRefreshPolicy
    {
        internal static List<Guid> ChooseMarked(IEnumerable<Guid> cached,
            IEnumerable<Guid> marked)
        {
            if (cached == null || marked == null) return new List<Guid>();
            var selection = new HashSet<Guid>(marked.Where(x => x != Guid.Empty));
            return cached.Where(x => x != Guid.Empty && selection.Contains(x))
                .Distinct().Take(WorkspaceScanSelection.BatchLimit).ToList();
        }

        internal static bool SameIdentity(string previousId, string previousName,
            string refreshedId, string refreshedName)
        {
            long before, after;
            return Int64.TryParse(previousId, out before) && before > 0 &&
                Int64.TryParse(refreshedId, out after) && after == before &&
                !String.IsNullOrWhiteSpace(previousName) &&
                String.Equals(previousName, refreshedName, StringComparison.Ordinal);
        }
    }
}
