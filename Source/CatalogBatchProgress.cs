using System;

namespace SmartMetadataMatcher
{
    // Pure presentation logic: no Playnite state, networking or writes.
    internal static class CatalogBatchProgress
    {
        internal static string Describe(int total,int applied,int skipped,int failed,string current,
            bool stopRequested,bool recoveryRequired)
        {
            if(total<1 || applied<0 || skipped<0 || failed<0 ||
                (long)applied+skipped+failed>total)
                throw new ArgumentOutOfRangeException("Batch counters must be in range.");
            var text=(applied+skipped+failed)+" / "+total+" checked  •  "+applied+
                " applied  •  "+skipped+" skipped  •  "+failed+" failed";
            if(recoveryRequired)return text+"  •  RECOVERY REQUIRED — no further games will run";
            if(stopRequested)return text+"  •  Stopping after the current game";
            if(!String.IsNullOrWhiteSpace(current))return text+"  •  "+current;
            return text;
        }
        // A bulk reviewed batch first validates *all* selected IGDB identities.
        // Its progress is distinct from applied/skipped metadata counts: no
        // Playnite writes have occurred during this phase.
        internal static string DescribeIdentityPreflight(int verified,int total,string current)
        {
            if(total<1 || verified<0 || verified>total)
                throw new ArgumentOutOfRangeException("Identity preflight counters must be in range.");
            var text="IGDB identity preflight: "+verified+" / "+total+
                " verified  •  No Playnite changes yet";
            if(!String.IsNullOrWhiteSpace(current))return text+"  •  "+current;
            return text;
        }
        // Older diagnostic callers retain their original signature.
        internal static string Describe(int total,int applied,int failed,string current,
            bool stopRequested,bool recoveryRequired)
        {
            return Describe(total,applied,0,failed,current,stopRequested,recoveryRequired);
        }
    }
}
