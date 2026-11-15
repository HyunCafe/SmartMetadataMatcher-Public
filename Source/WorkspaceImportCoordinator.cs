using System;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SmartMetadataMatcher
{
    // One shared commit boundary for Classic and future Design C callers.
    // Call only on the Playnite UI dispatcher. The existing writer remains the
    // single owner of staging, metadata writes, durable journals, and undo.
    internal static class WorkspaceImportCoordinator
    {
        internal static CatalogApplyResult ApplyOne(IPlayniteAPI api, EnrichmentJournal journal,
            WorkspaceReviewContract reviewed, Func<Game, CatalogInput> liveSnapshot,
            Guid gameId, string expectedName, string expectedPlatform,
            string expectedDescription, string expectedCover, string expectedIcon,
            string expectedBackground, string expectedSorting, CatalogGame catalog,
            byte[] cover, byte[] icon, byte[] background, bool rename,
            GameExtrasSnapshot expectedExtras, string approvedDisplayTitle,
            CatalogImportDraft draft = null)
        {
            if (api == null) throw new ArgumentNullException("api");
            if (journal == null) throw new ArgumentNullException("journal");
            if (reviewed != null)
            {
                if (liveSnapshot == null)
                    throw new InvalidOperationException("Live Playnite snapshot provider required for reviewed import.");
                // Read Playnite *now*, rather than trusting the UI's earlier cached
                // row. Only this game is checked, so previously completed games
                // in a sequential batch do not invalidate remaining approvals.
                var liveGame = api.Database.Games[gameId];
                reviewed.EnsureOneCurrent(gameId,
                    liveGame == null ? null : liveSnapshot(liveGame),
                    catalog == null ? 0 : catalog.Id,
                    catalog == null ? null : catalog.Name,
                    approvedDisplayTitle, draft);
            }
            // The original writer again checks the game, provider platform,
            // expected fields and recovery ledger before changing anything.
            return CatalogEnrichmentWriter.ApplyOne(api, journal, gameId,
                expectedName, expectedPlatform, expectedDescription, expectedCover,
                expectedIcon, expectedBackground, expectedSorting, catalog,
                cover, icon, background, rename, expectedExtras,
                approvedDisplayTitle, draft);
        }
    }
}
