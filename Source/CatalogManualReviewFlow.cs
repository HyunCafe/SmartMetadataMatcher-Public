using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // A checked row identifies a proposed decision; it is not yet approval.
    // Manual bulk approval is explicit, bounded, and rejected in its entirety
    // for stale/corrupt/conflicting selections before any identity file changes.
    internal sealed class CatalogManualReviewCandidate
    {
        internal Guid GameId;
        internal string GameName, Platform, CatalogTitle, CatalogId, PlannerCode;
        internal bool Archived, FromSavedScan, FromHistory, PriorFailure;
    }

    internal static class CatalogManualReviewFlow
    {
        internal const int MaxReviewedBatch=50;

        internal static string Validate(IEnumerable<CatalogManualReviewCandidate> source)
        {
            if(source==null)return "No games were selected";
            var rows=source.ToList();
            if(rows.Count==0)return "No games were selected";
            if(rows.Count>MaxReviewedBatch)
                return "Manual identity review is limited to 50 games per approval; select fewer games";
            var seen=new HashSet<Guid>();
            foreach(var row in rows)
            {
                long id;
                if(row==null || row.GameId==Guid.Empty || !seen.Add(row.GameId))
                    return "Duplicate or missing Playnite game identity";
                if(!Int64.TryParse(row.CatalogId,out id) || id<=0 ||
                    String.IsNullOrWhiteSpace(row.CatalogTitle) ||
                    String.IsNullOrWhiteSpace(row.GameName) || String.IsNullOrWhiteSpace(row.Platform))
                    return "A selected game has no complete IGDB ID, title or platform";
                if(row.Archived || row.FromHistory || row.PriorFailure)
                    return "A completed, ignored or previously failed game needs separate recovery or review";
                if(row.FromSavedScan)
                    return "A saved match must be refreshed by scanning live before bulk confirmation";
                if(row.PlannerCode=="ConflictingCatalogId" || row.PlannerCode=="InvalidGame" ||
                   row.PlannerCode=="PriorFailure" || row.PlannerCode=="Completed" ||
                   row.PlannerCode=="Ignored" || row.PlannerCode=="CachedNeedsRefresh" ||
                   row.PlannerCode=="MissingIdentity")
                    return "A selected match has an unresolved identity or recovery conflict";
            }
            return null;
        }
    }
}
