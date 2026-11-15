#!/usr/bin/env python3
"""v0.5.45 read-only regression: shared import coordinator owns write boundary."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
contract=(root/'Source/WorkspaceReviewContract.cs').read_text(encoding='utf-8-sig')
classic=(root/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
coordinator=(root/'Source/WorkspaceImportCoordinator.cs').read_text(encoding='utf-8-sig')
tests=(root/'Tests/WorkspaceBatchReviewTests.cs').read_text(encoding='utf-8-sig')
checks={
 'contract per-game snapshot': 'internal void EnsureOneCurrent(Guid gameId, CatalogInput current, long igdbId,' in contract,
 'field-mask guard': 'current.MissingMask != expected.MissingMask' in contract,
 'release-year guard': 'current.LocalReleaseYear != expected.LocalReleaseYear' in contract,
 'exact IGDB ID guard': 'igdbId != expected.IgdbId' in contract,
 'approved title guard': 'expected.PlannedTitle' in contract,
 'draft guard': 'WorkspaceBatchLivePreflight.SameDraft(expected.Draft, draft)' in contract,
 'batch uses shared coordinator': 'WorkspaceImportCoordinator.ApplyOne(api,enrichmentJournal,' in classic and
   'stagedNativeReview, ScanInput, item.Row.GameId,' in classic,
 'single import uses shared coordinator': 'null, ScanInput, row.GameId,' in classic,
 'no direct Classic writer entry': 'CatalogEnrichmentWriter.ApplyOne(api,enrichmentJournal' not in classic,
 'no UI-owned contract write guard': 'stagedNativeReview.EnsureOneCurrent(item.Row.GameId,' not in classic,
 'coordinator checks live database': 'var liveGame = api.Database.Games[gameId];' in coordinator,
 'coordinator requires live snapshot': 'if (liveSnapshot == null)' in coordinator,
 'coordinator calls frozen review': 'reviewed.EnsureOneCurrent(gameId,' in coordinator,
 'coordinator delegates to existing writer': 'return CatalogEnrichmentWriter.ApplyOne(api, journal, gameId,' in coordinator,
 'only one writer invocation': coordinator.count('CatalogEnrichmentWriter.ApplyOne(')==1,
 'commit validation precedes writer': coordinator.index('reviewed.EnsureOneCurrent(gameId,') <
   coordinator.index('return CatalogEnrichmentWriter.ApplyOne('),
 'existing C# draft regression': 'changed opt-in draft blocked at write boundary' in tests,
 'existing C# identity regression': 'unreviewed game cannot be written through native contract' in tests,
 'existing C# snapshot regression': 'changed Playnite metadata gaps blocked at write boundary' in tests,
 'existing batch is UI-dispatched': 'window.Dispatcher.Invoke(new Action(()=>' in classic,
 'existing one-game is UI-dispatched': 'window.Dispatcher.BeginInvoke(new Action(()=>' in classic,
}
for label,yes in checks.items():
 if not yes:raise AssertionError('FAIL v0.5.45: '+label)
for forbidden in ('Database.Games.Update(', 'journal.Commit(', 'journal.Prepare(',
                  'MediaStaging.Begin(', 'File.WriteAllBytes('):
 if forbidden in coordinator:raise AssertionError('FAIL independent writer: '+forbidden)
print('PASS:',len(checks),'shared importer write-boundary invariants')
