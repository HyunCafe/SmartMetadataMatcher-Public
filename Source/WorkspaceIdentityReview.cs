using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace SmartMetadataMatcher
{
    // Manual IGDB choices are only lookup suggestions. Confirmation records a user's
    // identity decision, never grants permission to import or bypass live preflight.
    internal static class WorkspaceIdentityRules
    {
        internal static bool ValidSearch(string value)
        {
            return !String.IsNullOrWhiteSpace(value) && value.Trim().Length >= 2 &&
                value.Trim().Length <= 120 && !value.Any(Char.IsControl);
        }
        internal static bool SameSaved(CatalogScanRecord saved, string id, string title,
            CatalogInput current)
        {
            return saved != null && current != null && current.Id != Guid.Empty &&
                current.MissingCount > 0 &&
                String.Equals(saved.CatalogId, id, StringComparison.Ordinal) &&
                String.Equals(saved.Candidate, title, StringComparison.Ordinal) &&
                saved.GameId == current.Id &&
                String.Equals(saved.Name, current.Name, StringComparison.Ordinal) &&
                String.Equals(saved.Platform, current.Platform, StringComparison.Ordinal) &&
                saved.MissingCount == current.MissingCount &&
                saved.MissingMask.HasValue && saved.MissingMask.Value == current.MissingMask;
        }
        internal static CatalogScanRecord ManualRecord(CatalogInput current, CatalogGame exact,
            DateTime utcNow)
        {
            if (current == null || current.Id == Guid.Empty || current.MissingCount <= 0 ||
                String.IsNullOrWhiteSpace(current.Name) || current.Platform == null ||
                exact == null || exact.Id <= 0 || String.IsNullOrWhiteSpace(exact.Name) ||
                exact.Name.Length > 1024 || utcNow.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Invalid manual identity or live game state");
            return new CatalogScanRecord {
                GameId = current.Id, Name = current.Name, Platform = current.Platform,
                MissingCount = current.MissingCount, MissingMask = current.MissingMask,
                ScannedUtc = utcNow.ToString("o", CultureInfo.InvariantCulture),
                CatalogId = exact.Id.ToString(CultureInfo.InvariantCulture),
                Candidate = exact.Name, Status = "Review", Score = "",
                Evidence = "Manually chosen IGDB identity. Verify game, platform and edition. " +
                    "Not automatic match evidence or import approval.",
                AvailableMetadata = (String.IsNullOrWhiteSpace(exact.Summary) ? "" : "description ") +
                    (String.IsNullOrWhiteSpace(exact.CoverImageId) ? "" : "cover ") +
                    (String.IsNullOrWhiteSpace(exact.ScreenshotImageId) ? "" : "screenshot"),
                Summary = exact.Summary, CoverId = exact.CoverImageId,
                ScreenshotId = exact.ScreenshotImageId, Queries = "Manual exact IGDB ID",
                FoundCount = "1", ReleaseYear = exact.FirstReleaseYear <= 0 ? "" :
                    exact.FirstReleaseYear.ToString(CultureInfo.InvariantCulture),
                Alternatives = ""
            };
        }
    }

    internal sealed class WorkspaceIdentityReview
    {
        private readonly string dataPath;
        internal WorkspaceIdentityReview(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Plugin data required");
            dataPath = path;
        }
        internal List<CatalogGame> Search(string term, string clientId, string clientSecret,
            CancellationToken token)
        {
            if (!WorkspaceIdentityRules.ValidSearch(term))
                throw new ArgumentException("Enter 2–120 characters to search IGDB");
            using (var client = new IgdbReadOnly(clientId, clientSecret))
                return client.SearchVariants(new [] {term.Trim()}, token)
                    .Where(x => x != null && x.Id > 0 && !String.IsNullOrWhiteSpace(x.Name))
                    .GroupBy(x => x.Id).Select(x => x.First()).Take(25).ToList();
        }
        internal CatalogGame FetchExact(long id, string clientId, string clientSecret,
            CancellationToken token)
        {
            if (id <= 0) throw new ArgumentException("IGDB ID must be positive");
            using (var client = new IgdbReadOnly(clientId, clientSecret))
                return client.FetchById(id, token);
        }
        private CatalogScanRecord CheckedCache(CatalogInput live, string expectedId,
            string expectedTitle, out CatalogScanCache cache)
        {
            cache = new CatalogScanCache(dataPath);
            cache.Load(); // Fail closed if existing lookup file is damaged.
            CatalogScanRecord saved;
            if (!cache.TryGet(live, DateTime.UtcNow, out saved) ||
                !WorkspaceIdentityRules.SameSaved(saved, expectedId, expectedTitle, live))
                throw new InvalidOperationException("Saved IGDB identity or Playnite fields changed. Reload first.");
            return saved;
        }
        internal void StageChoice(CatalogInput live, string expectedId, string expectedTitle,
            CatalogGame fetched)
        {
            CatalogScanCache cache;
            CheckedCache(live, expectedId, expectedTitle, out cache);
            var record = WorkspaceIdentityRules.ManualRecord(live, fetched, DateTime.UtcNow);
            var confirmations = new CatalogConfirmedIdentities(dataPath);
            confirmations.Load(); // Preserve fail-closed handling of malformed decisions.
            // Invalidate ANY old explicit decision first. If cache writing fails, the
            // old ID can never inherit a confirmation from a partially changed choice.
            confirmations.Forget(live.Id);
            cache.SaveBatch(new [] {record}, DateTime.UtcNow);
        }
        internal void Confirm(CatalogInput live, string expectedId, string expectedTitle,
            CatalogGame exact)
        {
            long id;
            if (!Int64.TryParse(expectedId, out id) || id <= 0 || exact == null ||
                exact.Id != id || !String.Equals(exact.Name, expectedTitle, StringComparison.Ordinal))
                throw new InvalidOperationException("The live IGDB ID/title differs from the saved candidate. Refresh or correct first.");
            CatalogScanCache cache;
            CheckedCache(live, expectedId, expectedTitle, out cache);
            var confirmations = new CatalogConfirmedIdentities(dataPath);
            confirmations.Load();
            confirmations.Confirm(live.Id, live.Name, live.Platform, exact.Id,
                exact.Name, DateTime.UtcNow);
        }
        internal void Forget(CatalogInput live, string expectedId, string expectedTitle)
        {
            CatalogScanCache cache;
            CheckedCache(live, expectedId, expectedTitle, out cache);
            var confirmations = new CatalogConfirmedIdentities(dataPath);
            confirmations.Load();
            confirmations.Forget(live.Id);
        }
    }
}
