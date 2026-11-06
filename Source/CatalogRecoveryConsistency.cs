using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Cross-checks BOTH durable logs. A closed media handoff must have a corresponding
    // field-journal intent; an abandoned media stage must not claim a journal owner.
    // Legacy field-only imports have no media stage and remain valid.
    internal sealed class CatalogRecoveryReport
    {
        internal int MediaStages, OpenStages, HandOffs, CleanAbandons;
        internal int JournalOperations, PendingOperations;
        internal readonly List<string> Problems=new List<string>();
        internal bool AllowsWrites { get { return Problems.Count==0; } }
    }
    internal static class CatalogRecoveryConsistency
    {
        internal static CatalogRecoveryReport Inspect(IEnumerable<CatalogMediaStage> stages,
            IEnumerable<EnrichmentEntry> entries)
        {
            if(stages==null || entries==null)
                throw new ArgumentNullException("Both recovery journals are required.");
            var media=stages.ToList();
            var records=entries.ToList();
            var result=new CatalogRecoveryReport {MediaStages=media.Count,
                JournalOperations=records.Count};
            var byOperation=new Dictionary<Guid,EnrichmentEntry>();
            foreach(var item in records)
            {
                if(item==null || item.OperationId==Guid.Empty || item.GameId==Guid.Empty ||
                    byOperation.ContainsKey(item.OperationId))
                {
                    result.Problems.Add("Metadata journal contains an invalid or duplicate operation.");
                    continue;
                }
                byOperation.Add(item.OperationId,item);
                if(!item.Committed && !item.Undone)
                {
                    result.PendingOperations++;
                    result.Problems.Add("Unfinished metadata operation " + item.OperationId.ToString("N") + ".");
                }
            }
            var seen=new HashSet<Guid>();
            foreach(var stage in media)
            {
                if(stage==null || stage.OperationId==Guid.Empty || stage.GameId==Guid.Empty ||
                    !seen.Add(stage.OperationId))
                {
                    result.Problems.Add("Media ledger contains an invalid or duplicate operation.");
                    continue;
                }
                EnrichmentEntry owner;
                bool owned=byOperation.TryGetValue(stage.OperationId,out owner);
                if(!stage.Closed)
                {
                    result.OpenStages++;
                    result.Problems.Add("Unfinished artwork stage " + stage.OperationId.ToString("N") + ".");
                }
                else if(stage.Outcome=="H")
                {
                    result.HandOffs++;
                    if(!owned)
                        result.Problems.Add("Orphan artwork handoff " + stage.OperationId.ToString("N") +
                            " has no metadata journal entry. Restore and inspect private backups.");
                    else if(owner.GameId!=stage.GameId)
                        result.Problems.Add("Artwork/metadata game ID mismatch for operation " +
                            stage.OperationId.ToString("N") + ".");
                }
                else if(stage.Outcome=="A")
                {
                    result.CleanAbandons++;
                    if(owned)
                        result.Problems.Add("Cleanly abandoned artwork stage " +
                            stage.OperationId.ToString("N") + " also has a metadata entry.");
                }
                else result.Problems.Add("Unrecognized artwork stage outcome " +
                    stage.OperationId.ToString("N") + ".");
            }
            return result;
        }
    }
}
