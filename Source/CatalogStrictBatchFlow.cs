using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Read-only bridge from the strict planner to the primary dashboard action.
    // Marked checkboxes do not influence this selection. No identity threshold,
    // record, network request, approval, metadata field or journal is changed.
    internal static class CatalogStrictBatchFlow
    {
        internal static HashSet<Guid> EligibleIds(IEnumerable<EnrichmentCandidate> candidates,
            IEnumerable<EnrichmentPlanDecision> decisions)
        {
            var ready=new HashSet<Guid>((decisions??Enumerable.Empty<EnrichmentPlanDecision>())
                .Where(x=>x!=null && x.ReadyForPreflight).Select(x=>x.GameId));
            return new HashSet<Guid>((candidates??Enumerable.Empty<EnrichmentCandidate>())
                .Where(x=>x!=null && ready.Contains(x.GameId) && x.GameId!=Guid.Empty &&
                    x.StrictEvidence && !x.UserConfirmed && !x.FromSavedScan &&
                    !x.FromHistory && !x.Completed && !x.Ignored && !x.PriorFailure &&
                    String.Equals(x.Status,"Strong candidate",StringComparison.Ordinal))
                .Select(x=>x.GameId));
        }
    }
}
