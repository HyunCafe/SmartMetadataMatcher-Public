using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace SmartMetadataMatcher
{
    internal sealed class WorkspaceScanOutcome
    {
        internal int Completed;
        internal bool Canceled;
        internal string Error;
    }

    // The native Design C scanner uses Classic's ONE existing resolver and the
    // existing atomic catalog-cache/checkpoint format. This is not an importer.
    // No Playnite database, journal, identity confirmation, or approval writes.
    internal sealed class WorkspaceCatalogLookup
    {
        private readonly string dataPath;
        internal WorkspaceCatalogLookup(string pluginDataPath)
        {
            if (String.IsNullOrWhiteSpace(pluginDataPath))
                throw new ArgumentException("Plugin data path required");
            dataPath = pluginDataPath;
        }

        internal static IgdbConnectionResult TestConnection(string id, string secret,
            CancellationToken token)
        {
            using (var client = new IgdbReadOnly(id, secret))
                return client.TestConnection(token);
        }

        internal WorkspaceScanOutcome Run(IEnumerable<CatalogInput> inputs,
            string clientId, string clientSecret, CancellationToken token,
            Action<int, int> report)
        {
            if (inputs == null) throw new ArgumentNullException("inputs");
            if (String.IsNullOrWhiteSpace(clientId) || String.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException("Configure Twitch / IGDB credentials first");
            var queue = inputs.ToList();
            if (queue.Count == 0 || queue.Count > WorkspaceScanSelection.BatchLimit ||
                queue.Any(x => x == null || x.Id == Guid.Empty || x.MissingCount <= 0) ||
                queue.Select(x => x.Id).Distinct().Count() != queue.Count)
                throw new ArgumentException("Invalid or unbounded scan selection");

            var cache = new CatalogScanCache(dataPath);
            // Fail closed on damaged history. Never silently overwrite a bad cache.
            cache.Load();
            var checkpoints = new CatalogScanCheckpoint(cache);
            var outcome = new WorkspaceScanOutcome();
            try
            {
                using (var client = new IgdbReadOnly(clientId, clientSecret))
                {
                    foreach (var input in queue)
                    {
                        token.ThrowIfCancellationRequested();
                        var queries = CatalogCore.QueryVariants(input.Name, input.Platform);
                        var found = client.SearchVariants(queries.Select(q => q.Text), token);
                        // Reuse the identical Classic algorithm, including exact/prefix
                        // rival checks; never create a weaker second matcher.
                        var match = CatalogWindow.ResolveWithExactFallback(input, found, client, token);
                        var game = match.Game;
                        var record = new CatalogScanRecord {
                            GameId = input.Id, Name = input.Name, Platform = input.Platform,
                            MissingCount = input.MissingCount, MissingMask = input.MissingMask,
                            ScannedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                            Candidate = game == null ? "(none)" : game.Name,
                            Status = match.MatchStatus, Evidence = match.Reason,
                            CatalogId = game == null ? "" : game.Id.ToString(CultureInfo.InvariantCulture),
                            Score = match.Score.ToString("0.000", CultureInfo.InvariantCulture),
                            AvailableMetadata = game == null ? "" :
                                (String.IsNullOrWhiteSpace(game.Summary) ? "" : "description ") +
                                (String.IsNullOrWhiteSpace(game.CoverImageId) ? "" : "cover ") +
                                (String.IsNullOrWhiteSpace(game.ScreenshotImageId) ? "" : "screenshot"),
                            Summary = game == null ? null : game.Summary,
                            CoverId = game == null ? null : game.CoverImageId,
                            ScreenshotId = game == null ? null : game.ScreenshotImageId,
                            Queries = String.Join(" | ", queries.Select(q => q.Text)),
                            FoundCount = match.CandidatesFound.ToString(CultureInfo.InvariantCulture),
                            ReleaseYear = game == null || game.FirstReleaseYear == 0 ? "" :
                                game.FirstReleaseYear.ToString(CultureInfo.InvariantCulture),
                            Alternatives = match.Alternatives == null ? "" : String.Join(" | ",
                                match.Alternatives.Select(g => g.Name + " [" + g.Id + "; " +
                                    String.Join("/", (g.Platforms ?? new List<string>()).Take(2)) + "]"))
                        };
                        checkpoints.Add(record, DateTime.UtcNow);
                        outcome.Completed++;
                        if (report != null) report(outcome.Completed, queue.Count);
                        // Maintain the existing 340ms request spacing between games.
                        if (outcome.Completed < queue.Count && token.WaitHandle.WaitOne(340))
                            token.ThrowIfCancellationRequested();
                    }
                }
            }
            catch (OperationCanceledException) { outcome.Canceled = true; }
            catch (Exception ex) { outcome.Error = ex.Message; }
            // Save the final short tail after success, cancellation, or error.
            // Existing fully checkpointed records remain intact on any failure.
            try { checkpoints.Flush(DateTime.UtcNow); }
            catch (Exception ex)
            {
                outcome.Error = String.IsNullOrWhiteSpace(outcome.Error) ?
                    "Catalog cache could not be saved: " + ex.Message :
                    outcome.Error + "; cache save failed: " + ex.Message;
            }
            return outcome;
        }
    }
}
