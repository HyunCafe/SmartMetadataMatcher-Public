#!/usr/bin/env python3
"""No-match review recovery scenarios; source-only, never Playnite runtime evidence."""
from pathlib import Path
p = Path(__file__).resolve().parents[1]
ui = (p/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
review = ui.split('private void ShowMarkedPreview(Guid? focusedGameId)',1)[1].split('// Native multi-game import:',1)[0]
focused = ui.split('private void ReviewFocusedGame()',1)[1].split('private void ShowMarkedPreview()',1)[0]
search = ui.split('private bool ShowIdentitySearch(Guid selectedGameId, bool restoreReviewMark)',1)[1].split('private void StartConfirmIdentity()',1)[0]
inspector = ui.split('private void UpdateInspector()',1)[1].split('private void ',1)[0]
checks = {
    'unmatched candidate recognized without relying on status text':
        'private static bool NeedsManualIdentity(LibraryWorkspaceItem row)' in ui and
        '!Int64.TryParse(row.CachedCatalogId, out catalogId)' in ui and
        'String.IsNullOrWhiteSpace(row.CachedCatalogName)' in ui,
    'no-match selected review routes before building plan':
        review.index('var unresolved = chosen.FirstOrDefault(NeedsManualIdentity);') <
        review.index('plan = WorkspaceBatchReview.Preview(all, library);'),
    'marked review only touches selected scope':
        'WorkspaceReviewHandoff.ForReviewScope(markedIds, focusedGameId)' in review and
        'reviewScope.Filter(items, x => x.GameId)' in review,
    'unscanned status directs scan instead of blocked confirm':
        'var unscanned = chosen.FirstOrDefault(x => !x.FromSaved);' in review and
        'Scan required before review' in review,
    'failed or canceled identity search stops review':
        'if (ShowIdentitySearch(unresolved.GameId, markedIds.Contains(unresolved.GameId)))' in review and
        'if (remaining == 0) ShowMarkedPreview(focusedGameId);' in review and
        'more marked game(s) need a match' in review,
    'focused review directly opens missing identity chooser':
        'if (NeedsManualIdentity(row))' in focused and
        'ShowIdentitySearch(row.GameId, false)' in focused,
    'focused action is context-aware':
        'reviewFocusedButton.IsEnabled = !BusyForNavigation() && (hasSavedId || needsMatch)' in inspector and
        '"Find IGDB match..." : "Review this game..."' in inspector,
    'identity search scoped by game id':
        'items.FirstOrDefault(x => x.GameId == selectedGameId)' in search and
        'if (BusyForNavigation() || selectedGameId == Guid.Empty) return false;' in search,
    'search uses existing discovery title only as hint':
        'CatalogCore.DiscoveryTitle(selected.Title, selected.Platform)' in search and
        'No search runs on opening.' in search,
    'manual selection still requires exact re-fetch and opt-in':
        'service.FetchExact(igdbId' in search and
        'MessageBoxButton.YesNo' in search and 'service.StageChoice(live' in search,
    'selected identity never returns as confirmed trust':
        'confirmations.Forget(live.Id)' in (p/'Source/WorkspaceIdentityReview.cs').read_text(encoding='utf-8-sig') and
        'No metadata will be imported.' in search,
    'restores review mark only on explicit review caller':
        'if (restoreReviewMark) markedIds.Add(selected.GameId);' in search and
        'NeedsManualIdentity(current)' in search and
        'return false;' in search,
    'return to existing scoped planner, not new writer':
        'ShowMarkedPreview(focusedGameId);' in review and
        'WorkspaceBatchReview.Preview(all, library)' in review and
        'StartNativeSingleImport(' in review and 'StartNativeConfirmedBatch(' in review,
    'same default manual search still available':
        'private void ShowIdentitySearch()' in ui and
        'ShowIdentitySearch(selected.GameId, false);' in ui,
    'right click review label reflects no match':
        'reviewOne.Header=NeedsManualIdentity(item)' in ui and
        '"Find IGDB match..." : "Review this game only..."' in ui,
    'no new writer or approval in identity recovery':
        all(x not in focused+search for x in
            ('Database.Games.Update(', 'CatalogEnrichmentWriter', '.MarkUndone(', 'new EnrichmentJournal(')),
}
for name, ok in checks.items():
    if not ok: raise AssertionError(name)
print('PASS:',len(checks),'contextual IGDB no-match recovery checks; Windows/Playnite unverified')
