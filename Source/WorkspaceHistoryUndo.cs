using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;

namespace SmartMetadataMatcher
{
    // History/undo stays behind the established enrichment journal and writer.
    // All methods are invoked on the Playnite UI dispatcher. No second journal,
    // file cleanup path, import writer, or independent undo algorithm is created.
    internal sealed class WorkspaceLatestUndoPreview
    {
        internal Guid OperationId;
        internal Guid GameId;
        internal string BeforeName;
        internal string AfterName;
        internal string Fields;
    }

    internal static class WorkspaceHistoryUndo
    {
        private static CatalogRecoveryReport CheckRecovery(EnrichmentJournal journal)
        {
            // BOTH ledgers must be parsable and consistent, including media staging.
            var report = CatalogRecoveryConsistency.Inspect(
                journal.MediaStaging.AllStages(), journal.AllEntries());
            if (!report.AllowsWrites)
                throw new InvalidOperationException("Recovery is required before undo: " +
                    String.Join("; ", report.Problems));
            return report;
        }

        internal static CatalogRecoveryReport InspectRecovery(string dataPath)
        {
            if (String.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException("Plugin data directory is required.");
            var journal = new EnrichmentJournal(dataPath);
            return CatalogRecoveryConsistency.Inspect(
                journal.MediaStaging.AllStages(), journal.AllEntries());
        }

        internal static WorkspaceLatestUndoPreview PreviewLatest(string dataPath)
        {
            if (String.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException("Plugin data directory is required.");
            var journal = new EnrichmentJournal(dataPath);
            CheckRecovery(journal);
            // Never undo an uncommitted prepare, a previously undone entry,
            // or an older operation while newer operations are still applied.
            var latest = journal.LatestOpen();
            if (latest == null) return null;
            if (!latest.Committed || latest.Undone || latest.OperationId == Guid.Empty)
                throw new InvalidOperationException("Latest journal entry is not a completed import. Inspect recovery.");
            return new WorkspaceLatestUndoPreview {
                OperationId = latest.OperationId, GameId = latest.GameId,
                BeforeName = latest.BeforeName, AfterName = latest.AfterName,
                Fields = CatalogEnrichmentWriter.Summary(latest)
            };
        }

        internal static List<WorkspaceLatestUndoPreview> PreviewSelected(string dataPath,
            IEnumerable<Guid> requestedOperationIds)
        {
            if (String.IsNullOrWhiteSpace(dataPath))
                throw new ArgumentException("Plugin data directory is required.");
            var requested = (requestedOperationIds ?? Enumerable.Empty<Guid>()).ToList();
            var journal = new EnrichmentJournal(dataPath);
            CheckRecovery(journal);
            var active = journal.Committed(); // Newest first; excludes undone entries.
            if (!EnrichmentPlan.IsLatestUndoPrefix(active.Select(x => x.OperationId),requested))
                throw new InvalidOperationException("Select only consecutive newest applied operations, " +
                    "starting with the latest import. Older, duplicate, or undone entries are blocked.");
            return active.Take(requested.Count).Select(x => new WorkspaceLatestUndoPreview {
                OperationId=x.OperationId, GameId=x.GameId,
                BeforeName=x.BeforeName, AfterName=x.AfterName,
                Fields=CatalogEnrichmentWriter.Summary(x)
            }).ToList();
        }

        internal static string UndoSelected(IPlayniteAPI api, string dataPath,
            IEnumerable<Guid> approvedOperationIds)
        {
            if (api == null) throw new ArgumentNullException("api");
            var approved = (approvedOperationIds ?? Enumerable.Empty<Guid>()).ToList();
            // The requested identities must still be the exact newest-first
            // prefix AFTER user approval. No stale selection grants permission.
            var preview = PreviewSelected(dataPath,approved);
            var journal = new EnrichmentJournal(dataPath);
            int completed = 0;
            try
            {
                foreach (var expected in preview)
                {
                    // Reopen BOTH ledgers and compare the top operation before
                    // EACH step. The writer checks every field for manual edits.
                    var latest = PreviewLatest(dataPath);
                    if (latest == null || latest.OperationId != expected.OperationId)
                        throw new InvalidOperationException("Journal order changed; reopen History.");
                    if (api.Database.Games[expected.GameId] == null)
                        throw new InvalidOperationException("The original Playnite game was deleted.");
                    CatalogEnrichmentWriter.UndoLast(api,journal);
                    completed++;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Stopped after " + completed + " of " + preview.Count +
                    " selected operation(s). Earlier undos remain recorded; later ones were not attempted. " +
                    ex.Message,ex);
            }
            return "Restored " + completed + " imported operation(s), newest first. " +
                "Playnite metadata was restored where safely unchanged; ROMs were untouched.";
        }

        internal static string UndoLatest(IPlayniteAPI api, string dataPath, Guid approvedOperationId)
        {
            if (api == null) throw new ArgumentNullException("api");
            if (approvedOperationId == Guid.Empty)
                throw new InvalidOperationException("No approved undo operation.");
            // Reopen the journal and recovery ledgers AFTER approval. A journal
            // change between preview and commit invalidates consent.
            var preview = PreviewLatest(dataPath);
            if (preview == null || preview.OperationId != approvedOperationId)
                throw new InvalidOperationException("The most recent import changed since undo approval. Reopen history.");
            var game = api.Database.Games[preview.GameId];
            if (game == null) throw new InvalidOperationException("The original Playnite game no longer exists.");
            // Writer itself checks EVERY current field against its journaled
            // after-value, refuses to overwrite manual edits, and records undo.
            return CatalogEnrichmentWriter.UndoLast(api, new EnrichmentJournal(dataPath));
        }
    }
}
