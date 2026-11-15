using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Transient proposal only: no durable permission, Playnite writes, or import-plan state.
    // Every eventual application must independently redo identity, live-field, backup,
    // journal and final-approval preflight in the established importer.
    internal sealed class WorkspaceDraftState
    {
        internal CatalogImportDraft Draft;
        internal int ExpectedMissingMask;
        internal int ExpectedReleaseYear;

        internal bool Matches(CatalogInput live, long id, string title)
        {
            return Draft != null && live != null && live.Id != Guid.Empty &&
                live.MissingMask == ExpectedMissingMask &&
                live.MissingCount == CatalogEnrichmentNeeds.Count(ExpectedMissingMask) &&
                live.LocalReleaseYear == ExpectedReleaseYear &&
                Draft.Matches(live.Id, live.Name, live.Platform, id, title);
        }
    }

    internal static class WorkspaceDraftPolicy
    {
        internal static readonly string[] OptionalFields = {
            "Description", "Cover", "Icon", "Background", "Release date",
            "Genres", "Developers", "Publishers", "Series"
        };

        internal static CatalogImportDraft New(CatalogInput live, WorkspaceLiveComparisonResult compared)
        {
            CheckComparison(live, compared);
            return new CatalogImportDraft {
                GameId = live.Id, IgdbId = compared.IgdbId,
                ExpectedName = live.Name, ExpectedPlatform = live.Platform,
                ExpectedCatalogTitle = compared.IgdbName
            };
        }

        internal static void CheckComparison(CatalogInput live, WorkspaceLiveComparisonResult compared)
        {
            if (live == null || compared == null || live.Id == Guid.Empty ||
                live.MissingMask <= 0 || live.MissingMask != compared.MissingMask ||
                live.MissingCount != CatalogEnrichmentNeeds.Count(live.MissingMask) ||
                !String.Equals(live.Name, compared.GameName, StringComparison.Ordinal) ||
                !String.Equals(live.Platform, compared.Platform, StringComparison.Ordinal) ||
                compared.GameId != live.Id || compared.IgdbId <= 0 ||
                String.IsNullOrWhiteSpace(compared.IgdbName))
                throw new InvalidOperationException("Live field evidence no longer matches this game and identity. Recheck before editing.");
        }

        internal static WorkspaceDraftState Save(CatalogInput live, WorkspaceLiveComparisonResult compared,
            CatalogImportDraft proposed, IEnumerable<Tuple<Guid, string, string>> library)
        {
            CheckComparison(live, compared);
            if (proposed == null || !proposed.Matches(live.Id, live.Name, live.Platform,
                    compared.IgdbId, compared.IgdbName) || !CatalogImportDraft.Valid(proposed))
                throw new InvalidOperationException("The draft no longer matches the reviewed game or contains an invalid title/description.");
            var title = CatalogImportDraft.ResolveTitle(live.Name, compared.IgdbName, live.Id,
                live.Platform, library, false, proposed);
            if (String.IsNullOrWhiteSpace(title))
                throw new InvalidOperationException("Proposed title is invalid or collides with another Playnite title.");
            // No write permission is stored; always clone so dialog controls cannot
            // modify a previously accepted draft through a shared reference.
            return new WorkspaceDraftState { Draft = proposed.Copy(),
                ExpectedMissingMask = live.MissingMask,
                ExpectedReleaseYear = live.LocalReleaseYear };
        }

        internal static bool MayInclude(WorkspaceLiveComparisonResult compared, string field)
        {
            return compared != null && compared.Fields.Any(x =>
                x.MayAdd && String.Equals(x.Field, field, StringComparison.Ordinal));
        }

        internal static bool Included(CatalogImportDraft draft, string field)
        {
            if (draft == null) return false;
            switch (field)
            {
                case "Description": return draft.Description;
                case "Cover": return draft.Cover;
                case "Icon": return draft.Icon;
                case "Background": return draft.Background;
                case "Release date": return draft.Release;
                case "Genres": return draft.Genres;
                case "Developers": return draft.Developers;
                case "Publishers": return draft.Publishers;
                case "Series": return draft.Series;
                default: return false;
            }
        }

        internal static void SetIncluded(CatalogImportDraft draft, string field, bool included)
        {
            if (draft == null) throw new ArgumentNullException("draft");
            switch (field)
            {
                case "Description": draft.Description = included; break;
                case "Cover": draft.Cover = included; break;
                case "Icon": draft.Icon = included; break;
                case "Background": draft.Background = included; break;
                case "Release date": draft.Release = included; break;
                case "Genres": draft.Genres = included; break;
                case "Developers": draft.Developers = included; break;
                case "Publishers": draft.Publishers = included; break;
                case "Series": draft.Series = included; break;
                default: throw new ArgumentException("Unsupported draft field: " + field);
            }
        }

        internal static string ActionLabel(WorkspaceLiveFieldRow field, CatalogImportDraft draft)
        {
            if (field == null) return "";
            if (draft == null || !field.MayAdd || Included(draft, field.Field))
                return field.PossibleAction;
            return "Excluded from this draft · " + field.PossibleAction;
        }

        internal static int IncludedAvailable(WorkspaceLiveComparisonResult compared, CatalogImportDraft draft)
        {
            return compared == null || draft == null ? 0 : compared.Fields.Count(x =>
                x.MayAdd && Included(draft, x.Field));
        }
    }
}
