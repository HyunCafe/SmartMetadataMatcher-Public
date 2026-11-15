using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    // First native Design C execution route: one explicitly confirmed game.
    // Network I/O runs off-thread. Only the existing coordinator/writer may
    // change Playnite, and only on its UI dispatcher after fresh verification.
    internal sealed class WorkspaceNativeApprovedSnapshot
    {
        internal Guid GameId;
        internal string Name, Platform, Description, Cover, Icon, Background, Sorting;
        internal GameExtrasSnapshot Extras;
        internal static WorkspaceNativeApprovedSnapshot Capture(Game game)
        {
            if (game == null) throw new InvalidOperationException("The Playnite game no longer exists.");
            return new WorkspaceNativeApprovedSnapshot {
                GameId=game.Id, Name=game.Name,
                Platform=game.Platforms == null ? "" :
                    String.Join(", ",game.Platforms.Select(x=>x.Name)),
                Description=game.Description, Cover=game.CoverImage,
                Icon=game.Icon, Background=game.BackgroundImage,
                Sorting=game.SortingName, Extras=GameExtrasSnapshot.Capture(game)
            };
        }
        internal bool Matches(Game game)
        {
            return game!=null && game.Id==GameId &&
                String.Equals(game.Name,Name,StringComparison.Ordinal) &&
                String.Equals(game.Platforms==null?"":
                    String.Join(", ",game.Platforms.Select(x=>x.Name)),Platform,
                    StringComparison.Ordinal) &&
                EnrichmentPlan.IsExpected(Description,game.Description) &&
                EnrichmentPlan.IsExpected(Cover,game.CoverImage) &&
                EnrichmentPlan.IsExpected(Icon,game.Icon) &&
                EnrichmentPlan.IsExpected(Background,game.BackgroundImage) &&
                EnrichmentPlan.IsExpected(Sorting,game.SortingName) &&
                Extras!=null && Extras.Matches(game);
        }
    }

    internal static class WorkspaceNativeSingleImport
    {
        private static void RequireConfirmed(string dataPath, CatalogInput input,
            long catalogId, string catalogTitle)
        {
            var decisions = new CatalogConfirmedIdentities(dataPath);
            decisions.Load();
            if (!decisions.TryMatch(input.Id, input.Name, input.Platform,
                catalogId, catalogTitle))
                throw new InvalidOperationException(
                    "The exact IGDB identity is not currently confirmed. Confirm the game and edition before importing.");
        }
        // For freshly and independently strict-resolved identities only. This
        // never writes a user-confirmation record or bypasses final approval.
        internal static CatalogApplyResult ExecuteStrict(IPlayniteAPI api, string dataPath,
            WorkspaceReviewContract review, Func<Game,CatalogInput> liveSnapshot,
            WorkspaceNativeApprovedSnapshot approved, Dispatcher dispatcher, Guid gameId, long catalogId, string catalogTitle,
            string approvedTitle, CatalogImportDraft draft, string clientId,
            string clientSecret, CancellationToken token)
        {
            return ExecuteCore(api,dataPath,review,liveSnapshot,approved,dispatcher,
                gameId,catalogId,catalogTitle,approvedTitle,draft,clientId,clientSecret,token,true);
        }
        internal static CatalogApplyResult Execute(IPlayniteAPI api, string dataPath,
            WorkspaceReviewContract review, Func<Game,CatalogInput> liveSnapshot,
            WorkspaceNativeApprovedSnapshot approved, Dispatcher dispatcher, Guid gameId, long catalogId, string catalogTitle,
            string approvedTitle, CatalogImportDraft draft, string clientId,
            string clientSecret, CancellationToken token)
        {
            return ExecuteCore(api,dataPath,review,liveSnapshot,approved,dispatcher,
                gameId,catalogId,catalogTitle,approvedTitle,draft,clientId,clientSecret,token,false);
        }
        private static CatalogApplyResult ExecuteCore(IPlayniteAPI api, string dataPath,
            WorkspaceReviewContract review, Func<Game,CatalogInput> liveSnapshot,
            WorkspaceNativeApprovedSnapshot approved, Dispatcher dispatcher, Guid gameId, long catalogId, string catalogTitle,
            string approvedTitle, CatalogImportDraft draft, string clientId,
            string clientSecret, CancellationToken token, bool requireFreshStrict)
        {
            if (api == null || dispatcher == null || liveSnapshot == null || review == null ||
                approved == null || approved.GameId != gameId || gameId == Guid.Empty || catalogId <= 0 ||
                String.IsNullOrWhiteSpace(catalogTitle) ||
                String.IsNullOrWhiteSpace(approvedTitle) ||
                String.IsNullOrWhiteSpace(clientId) ||
                String.IsNullOrWhiteSpace(clientSecret))
                throw new InvalidOperationException("Native import is missing its verified context.");
            token.ThrowIfCancellationRequested();
            CatalogInput approvedIdentity = null;
            dispatcher.Invoke(new Action(() => {
                var game = api.Database.Games[gameId];
                var current = game == null ? null : liveSnapshot(game);
                review.EnsureOneCurrent(gameId,current,catalogId,catalogTitle,approvedTitle,draft);
                if (!requireFreshStrict) RequireConfirmed(dataPath,current,catalogId,catalogTitle);
                else approvedIdentity = current;
                if (!approved.Matches(game))
                    throw new InvalidOperationException("Playnite metadata changed since final approval.");
            }));
            CatalogGame exact;
            byte[] cover = null, icon = null, background = null;
            using (var client = new IgdbReadOnly(clientId,clientSecret))
            {
                if (requireFreshStrict)
                {
                    // The in-memory mark is only a UI hint. Re-run ALL bounded
                    // rival searches through the original strict resolver now.
                    // This is independent of saved scores and persisted confirmations.
                    if (approvedIdentity == null || approvedIdentity.Id != gameId)
                        throw new InvalidOperationException("Strict identity snapshot is missing.");
                    var variants=CatalogCore.QueryVariants(approvedIdentity.Name,approvedIdentity.Platform);
                    var matches=client.SearchVariants(variants.Select(x=>x.Text),token);
                    var resolved=CatalogWindow.ResolveWithExactFallback(approvedIdentity,matches,client,token);
                    if (!CatalogCore.CanAutoPlan(resolved) || resolved.Game==null ||
                        resolved.Game.Id!=catalogId ||
                        !String.Equals(resolved.Game.Name,catalogTitle,StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "Fresh strict matching no longer uniquely verifies this exact IGDB game and edition. Review identity manually.");
                }
                exact = client.FetchById(catalogId,token);
                if (exact == null || exact.Id != catalogId ||
                    !String.Equals(exact.Name,catalogTitle,StringComparison.Ordinal) ||
                    CatalogCore.PlatformId(approved.Platform) <= 0 ||
                    exact.PlatformIds == null ||
                    !exact.PlatformIds.Contains(CatalogCore.PlatformId(approved.Platform)))
                    throw new InvalidOperationException(
                        "Live IGDB identity, title, or platform changed. Recheck before importing.");
                token.ThrowIfCancellationRequested();
                if ((draft == null || draft.Cover) && MetadataPlan.Missing(approved.Cover) &&
                    MetadataPlan.SafeImageId(exact.CoverImageId))
                    cover = client.DownloadCover(exact.CoverImageId,token);
                if ((draft == null || draft.Icon) && MetadataPlan.Missing(approved.Icon) &&
                    MetadataPlan.SafeImageId(exact.CoverImageId))
                {
                    try {icon = client.DownloadIcon(exact.CoverImageId,token);}
                    catch (OperationCanceledException) {throw;}
                    catch (Exception) { /* Optional artwork failure is not identity evidence. */ }
                }
                if ((draft == null || draft.Background) && MetadataPlan.Missing(approved.Background) &&
                    MetadataPlan.SafeImageId(exact.ScreenshotImageId))
                {
                    try {background = client.DownloadBackground(exact.ScreenshotImageId,token);}
                    catch (OperationCanceledException) {throw;}
                    catch (Exception) { /* Optional artwork failure leaves the field untouched. */ }
                }
            }
            token.ThrowIfCancellationRequested();
            CatalogApplyResult outcome = null;
            // Do not open a second writer, create a new journal format, or allow
            // cancellation to interrupt an already-started Playnite transaction.
            dispatcher.Invoke(new Action(() => {
                token.ThrowIfCancellationRequested();
                var latest = api.Database.Games[gameId];
                var current = latest == null ? null : liveSnapshot(latest);
                review.EnsureOneCurrent(gameId,current,catalogId,catalogTitle,approvedTitle,draft);
                if (!requireFreshStrict) RequireConfirmed(dataPath,current,catalogId,catalogTitle);
                else if (approvedIdentity == null || current == null ||
                    current.Id != approvedIdentity.Id ||
                    !String.Equals(current.Name,approvedIdentity.Name,StringComparison.Ordinal) ||
                    !String.Equals(current.Platform,approvedIdentity.Platform,StringComparison.Ordinal) ||
                    current.MissingMask != approvedIdentity.MissingMask ||
                    current.MissingCount != approvedIdentity.MissingCount ||
                    current.LocalReleaseYear != approvedIdentity.LocalReleaseYear)
                    throw new InvalidOperationException("Strict-matched Playnite identity changed before commit.");
                if (!approved.Matches(latest))
                    throw new InvalidOperationException("Playnite metadata changed after final approval.");
                var journal = new EnrichmentJournal(dataPath);
                outcome = WorkspaceImportCoordinator.ApplyOne(api,journal,review,liveSnapshot,
                    gameId,approved.Name,approved.Platform,approved.Description,approved.Cover,
                    approved.Icon,approved.Background,approved.Sorting,exact,cover,icon,background,
                    true,approved.Extras,approvedTitle,draft);
            }));
            return outcome;
        }
    }
}
