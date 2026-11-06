using System;

namespace SmartMetadataMatcher
{
    // Pure presentation logic: no Playnite state, networking or writes.
    internal static class CatalogBatchProgress
    {
        internal static string Describe(int total,int succeeded,int failed,string current,
            bool stopRequested,bool recoveryRequired)
        {
            if(total<1 || succeeded<0 || failed<0 || succeeded+failed>total)
                throw new ArgumentOutOfRangeException("Batch counters must be in range.");
            var text=(succeeded+failed)+" / "+total+" checked  •  "+succeeded+
                " applied  •  "+failed+" failed";
            if(recoveryRequired)return text+"  •  RECOVERY REQUIRED — no further games will run";
            if(stopRequested)return text+"  •  Stopping after the current game";
            if(!String.IsNullOrWhiteSpace(current))return text+"  •  "+current;
            return text;
        }
    }
}
