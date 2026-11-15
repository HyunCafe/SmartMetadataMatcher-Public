using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    // Immutable-per-operation input, captured before a single batch approval.
    // Never interpret a selected row or cached score as import authorization.
    internal sealed class WorkspaceNativeBatchEntry
    {
        internal Guid GameId;
        internal long IgdbId;
        internal string CatalogTitle, ApprovedTitle;
        internal CatalogImportDraft Draft;
        // True ONLY for an ephemeral, fresh, independently strict-resolved match.
        // Recomputed at final approval; never inferred from cached Strong status.
        internal bool FreshStrict;
        internal WorkspaceNativeApprovedSnapshot Snapshot;
    }

    internal sealed class WorkspaceNativeBatchResult
    {
        internal int Total, Applied, Skipped, Failed;
        internal bool Canceled;
        internal string Failure;
        internal readonly List<Guid> ProcessedGameIds = new List<Guid>();

        internal static WorkspaceNativeBatchResult Fatal(int total, string reason)
        {
            return new WorkspaceNativeBatchResult {
                Total=total, Failed=1, Failure=reason ?? "Batch setup failed."
            };
        }
    }

    // Sequential, fail-fast orchestration. Only WorkspaceNativeSingleImport
    // invokes the existing shared coordinator/writer and durable journals.
    // A batch is NOT a transaction. Committed entries retain independent undo.
    internal static class WorkspaceNativeConfirmedBatch
    {
        internal static WorkspaceNativeBatchResult Execute(IPlayniteAPI api,
            string dataPath, WorkspaceReviewContract reviewed,
            Func<Game,CatalogInput> liveSnapshot, Dispatcher dispatcher,
            IList<WorkspaceNativeBatchEntry> entries, string clientId,
            string clientSecret, CancellationToken cancellation,
            Action<int,int,int,string> onProgress)
        {
            if (api==null || reviewed==null || liveSnapshot==null || dispatcher==null ||
                entries==null || entries.Count<2 ||
                entries.Count>WorkspaceReviewHandoff.MaximumSelectedGames ||
                reviewed.Count!=entries.Count ||
                String.IsNullOrWhiteSpace(clientId) ||
                String.IsNullOrWhiteSpace(clientSecret) ||
                entries.Any(x=>x==null || x.GameId==Guid.Empty || x.IgdbId<=0 ||
                    x.Snapshot==null || x.Snapshot.GameId!=x.GameId ||
                    String.IsNullOrWhiteSpace(x.CatalogTitle) ||
                    String.IsNullOrWhiteSpace(x.ApprovedTitle) ||
                    (x.Draft!=null && !CatalogImportDraft.Valid(x.Draft))) ||
                entries.Select(x=>x.GameId).Distinct().Count()!=entries.Count)
                throw new InvalidOperationException("Confirmed batch has incomplete or repeated game identities.");
            // Batch membership and every proposed title/draft are exact. This
            // read-only gate runs before any IGDB request or Playnite update.
            reviewed.EnsureSamePlan(entries.Select(x=>Tuple.Create(x.GameId,x.IgdbId,
                x.CatalogTitle,x.ApprovedTitle,x.Draft)));
            var result=new WorkspaceNativeBatchResult {Total=entries.Count};
            foreach (var entry in entries)
            {
                if (cancellation.IsCancellationRequested)
                {
                    result.Canceled=true;
                    break;
                }
                try
                {
                    // Rechecks user-confirmed identity on disk, live Playnite
                    // snapshot, exact IGDB ID/title/platform, and recovery ledger
                    // before calling the one established journaled writer.
                    var outcome=entry.FreshStrict ?
                        WorkspaceNativeSingleImport.ExecuteStrict(api,dataPath,
                            reviewed,liveSnapshot,entry.Snapshot,dispatcher,
                            entry.GameId,entry.IgdbId,entry.CatalogTitle,
                            entry.ApprovedTitle,entry.Draft,clientId,clientSecret,
                            cancellation) :
                        WorkspaceNativeSingleImport.Execute(api,dataPath,
                            reviewed,liveSnapshot,entry.Snapshot,dispatcher,
                            entry.GameId,entry.IgdbId,entry.CatalogTitle,
                            entry.ApprovedTitle,entry.Draft,clientId,clientSecret,
                            cancellation);
                    if (outcome==null)
                        throw new InvalidOperationException("Native importer returned no result.");
                    if (outcome.NoChanges) result.Skipped++;
                    else result.Applied++;
                    result.ProcessedGameIds.Add(entry.GameId);
                    if (onProgress!=null)
                    {
                        try {onProgress(result.ProcessedGameIds.Count,result.Total,
                            result.Applied,entry.Snapshot.Name);}
                        catch (Exception) { /* UI progress cannot rewrite a committed outcome. */ }
                    }
                }
                catch (OperationCanceledException)
                {
                    // A cancellation is checked before the next write. No
                    // already-started journaled transaction is interrupted.
                    result.Canceled=true;
                    break;
                }
                catch (Exception ex)
                {
                    // Stop on the first failure, regardless of whether the
                    // journal recovery gate can still allow new operations.
                    result.Failed=1;
                    result.Failure=entry.Snapshot.Name+": "+ex.Message;
                    break;
                }
            }
            return result;
        }
    }
}
