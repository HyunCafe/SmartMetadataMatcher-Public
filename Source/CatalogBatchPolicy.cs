using System;

namespace SmartMetadataMatcher
{
    // The same explicit limits govern Review Matches and RunBatch.
    // Larger batches require independently strict candidate evidence.
    // Manually confirmed identities remain on a smaller supervised track.
    internal static class CatalogBatchPolicy
    {
        internal const int StrictLimit = 25;
        internal const int SupervisedLimit = 5;

        internal static bool IsStrictBatch(bool anyBlocked, bool anyUserConfirmed, bool manualSingle)
        {
            return !anyBlocked && !anyUserConfirmed && !manualSingle;
        }

        internal static int Limit(bool strictBatch)
        {
            return strictBatch ? StrictLimit : SupervisedLimit;
        }

        internal static bool WithinLimit(int selected, bool strictBatch)
        {
            return selected > 0 && selected <= Limit(strictBatch);
        }

        internal static int AvailableSelectionSlots(int alreadyMarked, bool includesConfirmed)
        {
            return Math.Max(0, Limit(!includesConfirmed)-Math.Max(0,alreadyMarked));
        }

        // Never continue to the next game after an unexpected write/preflight
        // error. Completed games keep their individual undo records.
        internal static bool StopOnFailure(bool encounteredError)
        {
            return encounteredError;
        }
    }
}
