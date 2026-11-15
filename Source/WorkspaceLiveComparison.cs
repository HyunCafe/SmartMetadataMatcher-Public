using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SmartMetadataMatcher
{
    // Read-only field availability evidence for ONE exact IGDB identity.
    // This is not an import plan. Images must still download as valid JPEGs,
    // shared Playnite entities must still be resolved, and every import must
    // pass the existing live preflight, backup and approval path.
    internal sealed class WorkspaceLiveFieldRow
    {
        internal string Field;
        internal string Current;
        internal string PossibleAction;
        internal bool MayAdd;
    }

    internal sealed class WorkspaceLiveComparisonResult
    {
        internal Guid GameId;
        internal string GameName;
        internal string Platform;
        internal int MissingMask;
        internal long IgdbId;
        internal string IgdbName;
        internal readonly List<WorkspaceLiveFieldRow> Fields = new List<WorkspaceLiveFieldRow>();
        internal int PossibleAdditions { get {return Fields.Count(x => x.MayAdd);} }
    }

    internal static class WorkspaceLiveComparison
    {
        private static string ShortNames(IEnumerable<string> source)
        {
            var values = (source ?? Enumerable.Empty<string>())
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .Select(x => String.Join(" ", x.Split(new [] {'\r','\n','\t'}, StringSplitOptions.RemoveEmptyEntries)).Trim())
                .Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (values.Count == 0) return "No IGDB value";
            var text = String.Join(", ", values.Take(3));
            if (text.Length > 95) text = text.Substring(0, 92) + "...";
            if (values.Count > 3) text += " (and " + (values.Count - 3) + " more)";
            return text;
        }
        private static void Add(WorkspaceLiveComparisonResult result, string name, int bit,
            string available, string value, string unavailable)
        {
            bool missing = (result.MissingMask & bit) != 0;
            // Existing supported data is never proposed for replacement.
            if (!missing && name != "Cover") return;
            result.Fields.Add(new WorkspaceLiveFieldRow {
                Field = name,
                Current = missing ? "Missing" : "Present",
                PossibleAction = !missing ? "Keep existing" :
                    available == "yes" ? value : unavailable,
                MayAdd = missing && available == "yes"
            });
        }
        internal static WorkspaceLiveComparisonResult Build(CatalogInput current,
            CatalogGame exact, long expectedId, string expectedTitle)
        {
            if (current == null || current.Id == Guid.Empty || current.MissingCount <= 0 ||
                current.MissingMask <= 0 || (current.MissingMask & ~CatalogEnrichmentNeeds.AllMask) != 0 ||
                current.MissingCount != CatalogEnrichmentNeeds.Count(current.MissingMask) ||
                String.IsNullOrWhiteSpace(current.Name) || current.Platform == null ||
                exact == null || expectedId <= 0 || exact.Id != expectedId ||
                String.IsNullOrWhiteSpace(expectedTitle) ||
                !String.Equals(exact.Name, expectedTitle, StringComparison.Ordinal))
                throw new InvalidOperationException("Live IGDB identity or Playnite metadata changed; reload and review again.");
            var result = new WorkspaceLiveComparisonResult {
                GameId = current.Id, GameName = current.Name, Platform = current.Platform,
                MissingMask = current.MissingMask, IgdbId = exact.Id, IgdbName = exact.Name
            };
            bool desc = !String.IsNullOrWhiteSpace(exact.Summary);
            string summary = desc ? String.Join(" ", exact.Summary.Split(new [] {'\r','\n','\t'},
                StringSplitOptions.RemoveEmptyEntries)).Trim() : "";
            if (summary.Length > 95) summary = summary.Substring(0, 92) + "...";
            Add(result,"Description",1,desc?"yes":"no",
                summary.Length == 0 ? "Summary found · verify on import" : summary,
                "Not supplied by IGDB");
            bool cover = MetadataPlan.SafeImageId(exact.CoverImageId);
            bool backdrop = MetadataPlan.SafeImageId(exact.ScreenshotImageId);
            Add(result,"Cover",2,cover?"yes":"no",
                "Image found · verify JPEG download on import", "No usable IGDB cover ID");
            Add(result,"Background",4,backdrop?"yes":"no",
                "Screenshot found · verify JPEG download on import", "No usable IGDB screenshot ID");
            Add(result,"Icon",8,cover?"yes":"no",
                "Cover image found · verify JPEG download on import", "No usable IGDB cover ID");
            long platformId = CatalogCore.PlatformId(current.Platform);
            var releaseYears = (exact.ReleaseDates ?? new List<CatalogRelease>())
                .Where(x => x != null && platformId > 0 && x.PlatformId == platformId &&
                    x.Year >= 1970 && x.Year <= DateTime.UtcNow.Year + 5)
                .Select(x => x.Year).ToList();
            Add(result,"Release date",16,releaseYears.Count > 0 ? "yes":"no",
                releaseYears.Count > 0 ? "Year " + releaseYears.Min().ToString(CultureInfo.InvariantCulture) +
                    " · for this platform" : "", "No matching platform release year");
            Add(result,"Genres",32,exact.Genres != null && exact.Genres.Any(x=>!String.IsNullOrWhiteSpace(x)) ? "yes":"no",
                ShortNames(exact.Genres),"No genres supplied by IGDB");
            Add(result,"Developers",64,exact.Developers != null && exact.Developers.Any(x=>!String.IsNullOrWhiteSpace(x)) ? "yes":"no",
                ShortNames(exact.Developers),"No developers supplied by IGDB");
            Add(result,"Publishers",128,exact.Publishers != null && exact.Publishers.Any(x=>!String.IsNullOrWhiteSpace(x)) ? "yes":"no",
                ShortNames(exact.Publishers),"No publishers supplied by IGDB");
            Add(result,"Series",256,exact.SeriesNames != null && exact.SeriesNames.Any(x=>!String.IsNullOrWhiteSpace(x)) ? "yes":"no",
                ShortNames(exact.SeriesNames),"No series supplied by IGDB");
            result.Fields.Add(new WorkspaceLiveFieldRow {Field="Display title",Current=current.Name,
                PossibleAction="Keep existing · renaming requires separate approval",MayAdd=false});
            return result;
        }
    }
}
