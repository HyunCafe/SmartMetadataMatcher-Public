using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // The same explicit limits govern Review Matches and RunBatch.
    // Larger batches require independently strict candidate evidence.
    // Manually confirmed identities remain on a smaller supervised track.
    internal sealed class CatalogBatchSelection
    {
        internal Guid GameId;
        internal string IgdbId, CatalogTitle;
        internal bool AutoReady, FromSavedScan, FromHistory, Ignored, Completed, UserConfirmed;
    }

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

        // Defensive final gate: checking the selection again in RunBatch avoids
        // trusting a stale checkmark or a changed review window. IGDB IDs can
        // legitimately repeat across DIFFERENT Playnite records. Only repeated
        // Playnite GameIds are rejected. No files or Playnite data are touched.
        internal static string ValidateSelection(IEnumerable<CatalogBatchSelection> rows,bool strictBatch)
        {
            if(rows==null)return "Missing reviewed selection";
            var items=rows.ToList();
            if(!WithinLimit(items.Count,strictBatch))
                return "Reviewed selection exceeds its batch limit or is empty";
            var seen=new HashSet<Guid>();
            foreach(var row in items)
            {
                if(row==null || row.GameId==Guid.Empty || !seen.Add(row.GameId))
                    return "Duplicate or invalid Playnite game in reviewed selection";
                long id;
                if(!Int64.TryParse(row.IgdbId,out id) || id<=0 ||
                   String.IsNullOrWhiteSpace(row.CatalogTitle))
                    return "Reviewed match has no valid IGDB identity";
                if(row.Ignored || row.Completed)
                    return "An ignored or already completed game is in the import selection";
                if(strictBatch && (!row.AutoReady || row.FromSavedScan ||
                   row.FromHistory || row.UserConfirmed))
                    return "Strict batch contains an unverified, cached or user-confirmed identity";
            }
            return null;
        }

        // Never continue to the next game after an unexpected write/preflight
        // error. Completed games keep their individual undo records.
        internal static bool StopOnFailure(bool encounteredError)
        {
            return encounteredError;
        }
    }
}
