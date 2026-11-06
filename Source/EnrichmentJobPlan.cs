using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Planning is pure and read-only. "Ready" means identity is eligible for
    // exact-ID revalidation and field preflight, NOT permission to write.
    // The WPF layer never infers identity trust from a displayed similarity score.
    internal sealed class EnrichmentCandidate
    {
        internal Guid GameId;
        internal string CurrentName, Platform, CandidateTitle, CatalogId;
        internal string Status;
        internal bool StrictEvidence, Completed, Ignored, FromHistory, Marked, PriorFailure;
    }

    internal sealed class EnrichmentPlanDecision
    {
        internal Guid GameId;
        internal string Code, Explanation;
        internal bool ReadyForPreflight, AllowRename;
        internal string RenameReason;
    }

    internal static class EnrichmentJobPlanner
    {
        internal static List<EnrichmentPlanDecision> Create(
            IEnumerable<EnrichmentCandidate> source,
            IEnumerable<Tuple<Guid,string,string>> library,
            bool requestRename, bool markedOnly)
        {
            var rows=(source??Enumerable.Empty<EnrichmentCandidate>())
                .Where(x=>x!=null).ToList();
            var names=(library??Enumerable.Empty<Tuple<Guid,string,string>>()).ToList();
            // Every currently known Playnite record with an IGDB ID participates
            // in duplicate detection, not merely the first N eligible rows.
            // Distinct GameIds avoid counting the same game twice if it is visible
            // in both history and the current scan.
            var repeatedIds=new HashSet<long>(rows.Select(x=>new { Row=x, Id=ParseId(x.CatalogId) })
                .Where(x=>x.Id>0 && x.Row.GameId!=Guid.Empty)
                .GroupBy(x=>x.Id)
                .Where(g=>g.Select(x=>x.Row.GameId).Distinct().Count()>1)
                .Select(g=>g.Key));
            var repeatedGames=new HashSet<Guid>(rows.Where(x=>x.GameId!=Guid.Empty)
                .GroupBy(x=>x.GameId).Where(g=>g.Count()>1).Select(g=>g.Key));
            var decisions=new List<EnrichmentPlanDecision>();
            foreach(var row in rows)
            {
                var decision=new EnrichmentPlanDecision {GameId=row.GameId,
                    Code="Review",Explanation="Identity requires review",RenameReason="Not evaluated"};
                long id=ParseId(row.CatalogId);
                if(markedOnly && !row.Marked)
                { decision.Code="NotSelected";decision.Explanation="Not marked for this plan"; }
                else if(row.GameId==Guid.Empty || repeatedGames.Contains(row.GameId))
                { decision.Code="InvalidGame";decision.Explanation="Duplicate or missing Playnite game identity"; }
                else if(row.Ignored)
                { decision.Code="Ignored";decision.Explanation="User chose to ignore this game"; }
                else if(row.Completed || row.FromHistory)
                { decision.Code="Completed";decision.Explanation="Already recorded as completed; re-enrichment requires fresh planning"; }
                else if(row.PriorFailure)
                { decision.Code="PriorFailure";decision.Explanation="Prior apply failed; inspect before retrying"; }
                else if(id<=0 || String.IsNullOrWhiteSpace(row.CandidateTitle))
                { decision.Code="MissingIdentity";decision.Explanation="No valid IGDB identity"; }
                else if(repeatedIds.Contains(id))
                { decision.Code="DuplicateCatalogId";decision.Explanation="IGDB ID shared by multiple scanned games; verify separate editions"; }
                else if(!String.Equals(row.Status,"Strong candidate",StringComparison.Ordinal) || !row.StrictEvidence)
                { decision.Code="NeedsReview";decision.Explanation="Strict independent title, platform and year evidence is incomplete"; }
                else
                {
                    decision.Code="ReadyForPreflight";
                    decision.Explanation="Revalidate exact IGDB ID and current fields before any update";
                    decision.ReadyForPreflight=true;
                    if(!requestRename) decision.RenameReason="Catalog-title rename not requested";
                    else if(!EnrichmentPlan.IsSafeTitle(row.CandidateTitle))
                        decision.RenameReason="Catalog title is not safe for a Playnite display name";
                    else if(String.Equals(row.CurrentName,row.CandidateTitle.Trim(),StringComparison.Ordinal))
                        decision.RenameReason="Already has the catalog title";
                    else if(EnrichmentPlan.HasCollision(row.CandidateTitle,row.GameId,row.Platform,names))
                        decision.RenameReason="Same-platform title collision; preserve the existing title";
                    else
                    {
                        decision.AllowRename=true;
                        decision.RenameReason="Title update permitted if live state still matches";
                    }
                }
                decisions.Add(decision);
            }
            return decisions;
        }

        private static long ParseId(string text)
        {
            long id;
            return Int64.TryParse(text,out id) && id>0 ? id : 0;
        }
    }
}
