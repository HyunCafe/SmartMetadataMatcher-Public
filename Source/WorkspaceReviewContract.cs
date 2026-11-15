using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Verified, read-only transfer contract between Design C and Classic.
    // It is neither final import approval nor permission to write any data.
    internal sealed class WorkspaceReviewContract
    {
        private sealed class Snapshot
        {
            internal readonly Guid GameId;
            internal readonly string GameName, Platform, CatalogTitle, PlannedTitle;
            internal readonly long IgdbId;
            internal readonly int MissingMask, LocalReleaseYear;
            internal readonly CatalogImportDraft Draft;
            internal readonly string VerifiedFieldPreview;

            internal Snapshot(WorkspaceBatchReviewRow row, WorkspaceLiveBatchTarget target,
                List<WorkspaceVerifiedFieldRow> verifiedFields)
            {
                GameId = target.GameId;
                GameName = target.GameName;
                Platform = target.Platform;
                CatalogTitle = target.CatalogTitle;
                PlannedTitle = row.TargetTitle;
                IgdbId = target.IgdbId;
                MissingMask = target.MissingMask;
                LocalReleaseYear = target.LocalReleaseYear;
                Draft = target.Draft == null ? null : target.Draft.Copy();
                // Defensive copy into an immutable review string. Never retain a
                // mutable WPF field row or treat availability as a committed write.
                VerifiedFieldPreview = String.Join("; ", verifiedFields
                    .Where(x => x.Field != "Display title")
                    .Select(x => x.Field + ": " + x.PlannedAction).ToArray());
            }
        }

        private readonly List<Snapshot> entries;
        private WorkspaceReviewContract(List<Snapshot> snapshots) { entries = snapshots; }
        internal int Count { get { return entries.Count; } }

        internal static WorkspaceReviewContract Capture(
            IEnumerable<WorkspaceBatchReviewRow> rows,
            IDictionary<Guid, WorkspaceLiveBatchTarget> verified,
            IDictionary<Guid, List<WorkspaceVerifiedFieldRow>> details)
        {
            if (!WorkspaceBatchLivePreflight.DetailedPlanReady(rows, verified, details))
                throw new InvalidOperationException("Native field review is missing, stale, or incomplete.");
            var source = rows.ToList();
            if (source.Count > WorkspaceReviewHandoff.MaximumSelectedGames)
                throw new InvalidOperationException("Too many games in this review selection.");
            var snapshots = new List<Snapshot>();
            foreach (var row in source)
            {
                var target = verified[row.GameId];
                if (row.GameId != target.GameId || target.IgdbId <= 0 ||
                    !String.Equals(row.Game, target.GameName, StringComparison.Ordinal) ||
                    !String.Equals(row.CurrentTitle, target.GameName, StringComparison.Ordinal) ||
                    !String.Equals(row.Platform, target.Platform, StringComparison.Ordinal) ||
                    String.IsNullOrWhiteSpace(target.CatalogTitle) ||
                    String.IsNullOrWhiteSpace(row.TargetTitle) ||
                    !EnrichmentPlan.IsSafeTitle(row.TargetTitle) ||
                    (target.Draft != null && (!CatalogImportDraft.Valid(target.Draft) ||
                        !target.Draft.Matches(target.GameId, target.GameName,
                            target.Platform, target.IgdbId, target.CatalogTitle))) ||
                    target.MissingMask < 0 ||
                    (target.MissingMask & ~CatalogEnrichmentNeeds.AllMask) != 0)
                    throw new InvalidOperationException("A reviewed identity, title, or draft differs from the live snapshot.");
                snapshots.Add(new Snapshot(row, target, details[row.GameId]));
            }
            // No two reviewed games may silently acquire the same proposed title.
            // External-library collisions remain checked by the existing planner/writer.
            if (snapshots.GroupBy(x => EnrichmentPlan.IdentityKey(x.PlannedTitle),
                StringComparer.Ordinal).Any(group => group.Count() > 1 &&
                    group.Any(x => !String.Equals(x.GameName, x.PlannedTitle,
                        StringComparison.Ordinal))))
                throw new InvalidOperationException("The reviewed batch contains colliding proposed display titles.");
            return new WorkspaceReviewContract(snapshots);
        }

        // Read-only, frozen text from the last exact provider inspection.
        // The actual writer will still retrieve and revalidate live data.
        internal string DescribeVerifiedFields(Guid gameId)
        {
            var snapshot = entries.SingleOrDefault(x => x.GameId == gameId);
            if (snapshot == null)
                throw new InvalidOperationException("The game was not included in the verified review.");
            return String.IsNullOrWhiteSpace(snapshot.VerifiedFieldPreview)
                ? "No additional fields available in exact IGDB review"
                : snapshot.VerifiedFieldPreview;
        }

        // Classic must not silently change native-approved *proposals* or
        // ignore selected games. This is NOT authorization to write: the
        // existing final confirmation, provider recheck, recovery and journal
        // are still mandatory.
        internal void EnsureSamePlan(IEnumerable<Tuple<Guid,long,string,string,CatalogImportDraft>> planned)
        {
            if (planned == null) throw new ArgumentNullException("planned");
            var proposed = planned.ToList();
            if (proposed.Count != entries.Count ||
                proposed.Any(x => x == null || x.Item1 == Guid.Empty) ||
                proposed.Select(x => x.Item1).Distinct().Count() != proposed.Count)
                throw new InvalidOperationException("The Classic selection differs from the verified native review. Recheck this batch.");
            var byId = entries.ToDictionary(x => x.GameId);
            foreach (var item in proposed)
            {
                Snapshot snapshot;
                if (!byId.TryGetValue(item.Item1, out snapshot) ||
                    snapshot.IgdbId != item.Item2 ||
                    !String.Equals(snapshot.CatalogTitle, item.Item3, StringComparison.Ordinal) ||
                    !String.Equals(snapshot.PlannedTitle, item.Item4, StringComparison.Ordinal) ||
                    !WorkspaceBatchLivePreflight.SameDraft(snapshot.Draft, item.Item5))
                    throw new InvalidOperationException("The IGDB identity, proposed title, or staged fields differ from the native review. Recheck the exact field plan.");
            }
        }

        // Per-game commit-boundary check. Earlier games in a sequential batch may
        // have completed and changed; never revalidate their pre-import snapshots
        // when deciding whether the next independent game is still safe to write.
        // Called on the Playnite UI thread immediately before the existing writer.
        internal void EnsureOneCurrent(Guid gameId, CatalogInput current, long igdbId,
            string catalogTitle, string plannedTitle, CatalogImportDraft draft)
        {
            var expected = entries.SingleOrDefault(x => x.GameId == gameId);
            if (expected == null || current == null || current.Id != gameId ||
                !String.Equals(current.Name, expected.GameName, StringComparison.Ordinal) ||
                !String.Equals(current.Platform, expected.Platform, StringComparison.Ordinal) ||
                current.MissingMask != expected.MissingMask ||
                current.MissingCount != CatalogEnrichmentNeeds.Count(expected.MissingMask) ||
                current.LocalReleaseYear != expected.LocalReleaseYear ||
                igdbId != expected.IgdbId ||
                !String.Equals(catalogTitle, expected.CatalogTitle, StringComparison.Ordinal) ||
                !String.Equals(plannedTitle, expected.PlannedTitle, StringComparison.Ordinal) ||
                !WorkspaceBatchLivePreflight.SameDraft(expected.Draft, draft))
                throw new InvalidOperationException(
                    "The game, verified identity, proposed title, or draft changed before writing. " +
                    "Import stopped before modifying this game; return to Design C and review again.");
        }

        // Called on the Playnite UI thread when Classic receives the review.
        // Classic's own final approval, exact live provider preflight, writer,
        // journals and undo continue to perform the authoritative validation.
        internal void EnsureCurrent(Func<Guid, CatalogInput> readCurrent)
        {
            if (readCurrent == null) throw new ArgumentNullException("readCurrent");
            if (entries.Count == 0)
                throw new InvalidOperationException("No verified games were transferred.");
            foreach (var expected in entries)
            {
                CatalogInput current = readCurrent(expected.GameId);
                if (current == null || current.Id != expected.GameId ||
                    !String.Equals(current.Name, expected.GameName, StringComparison.Ordinal) ||
                    !String.Equals(current.Platform, expected.Platform, StringComparison.Ordinal) ||
                    current.MissingMask != expected.MissingMask ||
                    current.MissingCount != CatalogEnrichmentNeeds.Count(expected.MissingMask) ||
                    current.LocalReleaseYear != expected.LocalReleaseYear)
                    throw new InvalidOperationException("The Playnite game changed after the native field review. Recheck before importing.");
            }
        }
    }
}
