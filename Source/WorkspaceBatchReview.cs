using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Pure, read-only snapshot of a Design C batch. No import authorization.
    internal sealed class WorkspaceBatchReviewInput
    {
        internal Guid GameId;
        internal string GameName, Platform, CandidateId, CandidateTitle, MatchStatus;
        internal int MissingMask, MissingCount, LocalReleaseYear;
        internal bool Marked, Saved, Confirmed, Completed, Ignored;
        // True only for fresh, session-local CatalogCore.CanAutoPlan evidence.
        internal bool StrictEvidence;
        internal WorkspaceDraftState DraftState;
    }

    internal sealed class WorkspaceBatchReviewRow
    {
        public string Game { get; set; }
        public string Platform { get; set; }
        public string Identity { get; set; }
        public string ProposedTitle { get; set; }
        public string CurrentTitle { get; set; }
        public string TargetTitle { get; set; }
        public string Draft { get; set; }
        public string NextStep { get; set; }
        public bool ReadyForPreflight { get; set; }
        public string LiveCheck { get; set; }
        public string AvailableFields { get; set; }
        public Guid GameId { get; set; }
    }

    // Read-only display record from a live exact IGDB field comparison.
    // These strings are proposals, not a committed Playnite write contract.
    internal sealed class WorkspaceVerifiedFieldRow
    {
        public Guid GameId { get; set; }
        public string Game { get; set; }
        public string Platform { get; set; }
        public string IgdbId { get; set; }
        public string Field { get; set; }
        public string Current { get; set; }
        public string Igdb { get; set; }
        public string PlannedAction { get; set; }
    }

    // Immutable-by-convention source snapshot for native read-only preflight.
    // It never carries import permission or owns a Playnite writer.
    internal sealed class WorkspaceLiveBatchTarget
    {
        internal Guid GameId;
        internal string GameName, Platform, CatalogTitle;
        internal long IgdbId;
        internal int MissingMask, LocalReleaseYear;
        internal CatalogImportDraft Draft;
    }

    internal static class WorkspaceBatchLivePreflight
    {
        // A completed read-only check allows TRANSFER only, never an import.
        // Each row must have been inspected successfully for this exact batch.
        internal static bool ReadyForHandoff(IEnumerable<WorkspaceBatchReviewRow> rows,
            IDictionary<Guid,WorkspaceLiveBatchTarget> verified)
        {
            if (rows == null || verified == null) return false;
            var selected = rows.ToList();
            return selected.Count > 0 && selected.Count == verified.Count &&
                selected.Select(x => x.GameId).Distinct().Count() == selected.Count &&
                selected.All(x => x.GameId != Guid.Empty &&
                    x.LiveCheck == "Fields checked · not approved" &&
                    x.NextStep != "Resolve title collision" &&
                    x.NextStep != "Recheck draft" &&
                    !String.IsNullOrWhiteSpace(x.ProposedTitle) &&
                    verified.ContainsKey(x.GameId) &&
                    verified[x.GameId] != null &&
                    verified[x.GameId].GameId == x.GameId);
        }
        // Every row in the itemized native review must originate from the
        // same successful exact field check. A stale/partial table cannot be
        // presented as verified, and cannot be used for handoff.
        internal static bool DetailedPlanReady(IEnumerable<WorkspaceBatchReviewRow> rows,
            IDictionary<Guid,WorkspaceLiveBatchTarget> verified,
            IDictionary<Guid,List<WorkspaceVerifiedFieldRow>> details)
        {
            if (!ReadyForHandoff(rows,verified) || details == null) return false;
            var selected = rows.ToList();
            if (details.Count != selected.Count) return false;
            foreach (var row in selected)
            {
                List<WorkspaceVerifiedFieldRow> fields;
                if (!details.TryGetValue(row.GameId,out fields) || fields == null ||
                    fields.Count == 0 || String.IsNullOrWhiteSpace(row.TargetTitle) ||
                    String.IsNullOrWhiteSpace(row.CurrentTitle)) return false;
                var checkedTarget = verified[row.GameId];
                if (fields.Any(x => x == null || x.GameId != row.GameId ||
                    !String.Equals(x.Game,checkedTarget.GameName,StringComparison.Ordinal) ||
                    !String.Equals(x.Platform,checkedTarget.Platform,StringComparison.Ordinal) ||
                    !String.Equals(x.IgdbId,checkedTarget.IgdbId.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),StringComparison.Ordinal) ||
                    String.IsNullOrWhiteSpace(x.Field) ||
                    String.IsNullOrWhiteSpace(x.PlannedAction))) return false;
                if (fields.Select(x=>x.Field).Distinct(StringComparer.Ordinal).Count()!=fields.Count)
                    return false;
                var titleRows=fields.Where(x=>x.Field=="Display title").ToList();
                if (titleRows.Count!=1 ||
                    !String.Equals(titleRows[0].Current,row.CurrentTitle,StringComparison.Ordinal) ||
                    !String.Equals(titleRows[0].PlannedAction,row.TargetTitle,StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
        internal static bool SameDraft(CatalogImportDraft first,CatalogImportDraft second)
        {
            if (first == null || second == null) return first == null && second == null;
            return first.GameId == second.GameId && first.IgdbId == second.IgdbId &&
                String.Equals(first.ExpectedName,second.ExpectedName,StringComparison.Ordinal) &&
                String.Equals(first.ExpectedPlatform,second.ExpectedPlatform,StringComparison.Ordinal) &&
                String.Equals(first.ExpectedCatalogTitle,second.ExpectedCatalogTitle,StringComparison.Ordinal) &&
                first.CustomTitleEnabled == second.CustomTitleEnabled &&
                first.CustomDescriptionEnabled == second.CustomDescriptionEnabled &&
                String.Equals(first.CustomTitle,second.CustomTitle,StringComparison.Ordinal) &&
                String.Equals(first.CustomDescription,second.CustomDescription,StringComparison.Ordinal) &&
                first.Description == second.Description && first.Cover == second.Cover &&
                first.Icon == second.Icon && first.Background == second.Background &&
                first.Release == second.Release && first.Genres == second.Genres &&
                first.Developers == second.Developers && first.Publishers == second.Publishers &&
                first.Series == second.Series;
        }
        internal static WorkspaceLiveBatchTarget Capture(CatalogInput game,long catalogId,
            string catalogTitle,CatalogImportDraft draft)
        {
            if (game == null || game.Id == Guid.Empty ||
                String.IsNullOrWhiteSpace(game.Name) || game.Platform == null ||
                catalogId <= 0 || String.IsNullOrWhiteSpace(catalogTitle) ||
                game.MissingMask < 0 ||
                (game.MissingMask & ~CatalogEnrichmentNeeds.AllMask) != 0 ||
                game.MissingCount != CatalogEnrichmentNeeds.Count(game.MissingMask))
                throw new InvalidOperationException("Game or catalog snapshot is incomplete.");
            if (draft != null && (!CatalogImportDraft.Valid(draft) ||
                draft.GameId != game.Id || draft.IgdbId != catalogId ||
                !String.Equals(draft.ExpectedName,game.Name,StringComparison.Ordinal) ||
                !String.Equals(draft.ExpectedPlatform,game.Platform,StringComparison.Ordinal) ||
                !String.Equals(draft.ExpectedCatalogTitle,catalogTitle,StringComparison.Ordinal)))
                throw new InvalidOperationException("The staged draft does not match this IGDB identity.");
            return new WorkspaceLiveBatchTarget {
                GameId=game.Id,GameName=game.Name,Platform=game.Platform,
                MissingMask=game.MissingMask,LocalReleaseYear=game.LocalReleaseYear,
                IgdbId=catalogId,CatalogTitle=catalogTitle,
                Draft=draft == null ? null : draft.Copy()
            };
        }
        internal static void EnsureCurrent(WorkspaceLiveBatchTarget expected,CatalogInput current,
            long catalogId,string catalogTitle)
        {
            if (expected == null || current == null || current.Id != expected.GameId ||
                catalogId != expected.IgdbId ||
                !String.Equals(catalogTitle,expected.CatalogTitle,StringComparison.Ordinal) ||
                !String.Equals(current.Name,expected.GameName,StringComparison.Ordinal) ||
                !String.Equals(current.Platform,expected.Platform,StringComparison.Ordinal) ||
                current.MissingMask != expected.MissingMask ||
                current.LocalReleaseYear != expected.LocalReleaseYear ||
                current.MissingCount != CatalogEnrichmentNeeds.Count(expected.MissingMask))
                throw new InvalidOperationException("Playnite game or IGDB suggestion changed during lookup.");
        }
    }

    internal static class WorkspaceBatchReview
    {
        internal static List<WorkspaceBatchReviewRow> Preview(
            IEnumerable<WorkspaceBatchReviewInput> source,
            IEnumerable<Tuple<Guid,string,string>> library)
        {
            if (source == null || library == null)
                throw new ArgumentNullException(source == null ? "source" : "library");
            var inputs = source.Where(x => x != null).ToList();
            if (inputs.Any(x => x.GameId == Guid.Empty || String.IsNullOrWhiteSpace(x.GameName) ||
                x.Platform == null || x.MissingMask < 0 ||
                (x.MissingMask & ~CatalogEnrichmentNeeds.AllMask) != 0 ||
                x.MissingCount != CatalogEnrichmentNeeds.Count(x.MissingMask)) ||
                inputs.Select(x => x.GameId).Distinct().Count() != inputs.Count)
                throw new InvalidOperationException("Library changed or has duplicate game IDs. Reload before batch review.");
            var selected = inputs.Where(x => x.Marked).ToList();
            if (selected.Count < 1 || selected.Count > WorkspaceReviewHandoff.MaximumSelectedGames ||
                selected.Any(x => x.Completed || x.Ignored || x.MissingCount <= 0))
                throw new InvalidOperationException("Select 1 to 1000 eligible games; updated or ignored games cannot enter review.");
            var names = library.ToList();
            // Reserve proposed titles for the entire marked batch, not just the
            // current Playnite library. This is still a read-only preview.
            var plannedNames = names.ToList();
            var candidates = inputs.Select(x => new EnrichmentCandidate {
                GameId = x.GameId, CurrentName = x.GameName, Platform = x.Platform,
                CandidateTitle = x.CandidateTitle, CatalogId = x.CandidateId,
                Status = x.MatchStatus, Marked = x.Marked, FromSavedScan = x.Saved,
                UserConfirmed = x.Confirmed, Completed = x.Completed, Ignored = x.Ignored,
                // Saved scan scores NEVER recreate trusted strict evidence.
                StrictEvidence = x.StrictEvidence
            }).ToList();
            var decisions = EnrichmentJobPlanner.Create(candidates, names, true, true);
            var byId = decisions.ToDictionary(x => x.GameId);
            var output = new List<WorkspaceBatchReviewRow>();
            foreach (var current in selected.OrderBy(x => x.GameName, StringComparer.OrdinalIgnoreCase))
            {
                EnrichmentPlanDecision decision = byId[current.GameId];
                long igdbId;
                bool hasId = Int64.TryParse(current.CandidateId, out igdbId) && igdbId > 0 &&
                    !String.IsNullOrWhiteSpace(current.CandidateTitle);
                CatalogImportDraft draft = null;
                bool staleDraft = false;
                if (current.DraftState != null)
                {
                    var live = new CatalogInput {Id=current.GameId,Name=current.GameName,
                        Platform=current.Platform,MissingMask=current.MissingMask,
                        MissingCount=current.MissingCount,LocalReleaseYear=current.LocalReleaseYear};
                    staleDraft = !hasId || !current.DraftState.Matches(live, igdbId,current.CandidateTitle);
                    if (!staleDraft) draft=current.DraftState.Draft.Copy();
                }
                string title = current.GameName;
                string titleNote = "Keep current title";
                bool titleValid = true;
                if (hasId)
                {
                    // Standard policy: clean Playnite display titles on approved import.
                    // ResolveTitle enforces custom-title validation and name collisions.
                    title = CatalogImportDraft.ResolveTitle(current.GameName,current.CandidateTitle,
                        current.GameId,current.Platform,plannedNames,true,draft);
                    titleValid = !String.IsNullOrWhiteSpace(title);
                    if (!titleValid)titleNote="Invalid / colliding title — edit draft";
                    else if (title == current.GameName && decision.RenameReason != null &&
                        decision.RenameReason.IndexOf("Ambiguous",StringComparison.OrdinalIgnoreCase)>=0)
                        titleNote="Keep original (duplicate title needs review)";
                    else if (title != current.GameName) titleNote=title;
                    else titleNote="Already clean / unchanged";
                    if (titleValid)
                    {
                        var index = plannedNames.FindIndex(x => x.Item1 == current.GameId);
                        if (index < 0)
                            throw new InvalidOperationException("A selected game disappeared from the library. Reload before reviewing.");
                        plannedNames[index] = Tuple.Create(current.GameId,title,current.Platform);
                    }
                }
                string draftNote = staleDraft ? "Stale draft — recheck exact fields" :
                    draft == null ? "Default missing-field additions" :
                    "Staged draft — transferred for Classic final review";
                bool eligible = decision.ReadyForPreflight && titleValid && !staleDraft;
                output.Add(new WorkspaceBatchReviewRow {
                    GameId=current.GameId, Game=current.GameName,Platform=current.Platform,
                    CurrentTitle=current.GameName,
                    TargetTitle=titleValid ? title : "",
                    Identity=current.Confirmed?"User-confirmed · recheck exact ID":
                        current.StrictEvidence?"Fresh strict evidence · verify exact ID":
                        hasId?"Suggested · verify before import":"No verified identity",
                    ProposedTitle=titleNote,Draft=draftNote,
                    NextStep=!titleValid?"Resolve title collision":staleDraft?"Recheck draft":
                        eligible?"Eligible for live preflight · not approved":decision.Explanation,
                    ReadyForPreflight=eligible,
                    LiveCheck="Not checked",AvailableFields="—"
                });
            }
            return output;
        }
    }
}
