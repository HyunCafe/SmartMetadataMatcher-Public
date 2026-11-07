using System;
using System.Globalization;

namespace SmartMetadataMatcher
{
    // Pure, read-only dashboard classification. Does not influence matching,
    // eligibility, metadata writes, journal recovery or user approvals.
    internal static class CatalogQueueView
    {
        // 0:Candidates 1:Review 2:Unmatched 3:No changes 4:Completed 5:Ignored
        // Tab 6 shows everything regardless of bucket.
        internal static int Bucket(string status,bool completed,bool ignored,bool selectable)
        {
            if(ignored)return 5;
            if(completed)return 4;
            if(String.Equals(status,"No changes available",StringComparison.Ordinal))return 3;
            if(String.Equals(status,"Unmatched",StringComparison.Ordinal))return 2;
            if(selectable)return 0;
            return 1;
        }
        internal static bool VisibleInTab(string status,bool completed,bool ignored,
            bool selectable,int selectedTab)
        {
            return selectedTab==6 || Bucket(status,completed,ignored,selectable)==selectedTab;
        }
        internal static bool ArchivedInReview(string status,string planCode)
        {
            // Presentation-only: a successful no-change preflight is not work
            // waiting for identity review. It remains accessible in All results.
            return status=="No changes available" || planCode=="Completed" || planCode=="Ignored";
        }
        internal static int ReviewPriority(int missingMask,string availableMetadata)
        {
            // Current scanned metadata fields provide opportunity hints only.
            // A high-priority row still needs strict IGDB identity review.
            return CatalogEnrichmentNeeds.CandidateOverlap(missingMask,availableMetadata)=="None"?1:0;
        }
        internal static bool HighScoreTitleMismatch(string title,string candidate,
            string platform,string score)
        {
            double value;
            if(String.IsNullOrWhiteSpace(title) || String.IsNullOrWhiteSpace(candidate) ||
                !Double.TryParse(score,NumberStyles.Float,CultureInfo.InvariantCulture,out value) ||
                Double.IsNaN(value) || value<0.99 || value>1.0)return false;
            var cleaned=CatalogCore.QueryTitle(title,platform);
            return !String.Equals(EnrichmentPlan.IdentityKey(cleaned),
                EnrichmentPlan.IdentityKey(candidate),StringComparison.Ordinal);
        }
        internal static string TitleMismatchWarning(string title,string candidate,
            string platform,string score,string evidence)
        {
            var platformConflict=(evidence??"").IndexOf("Platform mismatch",
                StringComparison.OrdinalIgnoreCase)>=0;
            var aliasConflict=HighScoreTitleMismatch(title,candidate,platform,score);
            if(!aliasConflict && !platformConflict)return "";
            return (platformConflict?"CAUTION: IGDB platform mismatch; verify the exact platform. ":"")+
                (aliasConflict?"CAUTION: High-score title discrepancy; a catalog alias or alternate edition may differ from the actual game. Match score is not a probability; verify identity manually.":"");
        }
    }
}
