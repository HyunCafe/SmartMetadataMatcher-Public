#!/usr/bin/env python3
"""Native opt-in full rescan: fail-closed cache archive, no Playnite metadata writes."""
from pathlib import Path
source = (Path(__file__).resolve().parent.parent/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
method = source.split('private void StartFullRescan()',1)
assert len(method)==2, 'Full rescan must have one implementation'
body = method[1].split('private void StartDirectScan()',1)[0]
checks = {
 'Advanced discoverability': 'ActionButton("Advanced"' in source and 'Rescan all IGDB matches (archive saved results)...' in source,
 'explicit confirmation': 'MessageBoxButton.YesNo' in body and 'answer != MessageBoxResult.Yes' in body,
 'no work during scan/import': all(x in body for x in ('scanning || checkingLiveFields', 'checkingBatchFields || nativeApplying', '!window.IsVisible')),
 'cache validation first': 'cache.Load();' in body and body.index('cache.Load();') < body.index('ArchiveAndClear(DateTime.UtcNow)'),
 'archive before reset': 'cache.ArchiveAndClear(DateTime.UtcNow)' in body,
 'never silently launch a scan': 'StartDirectScan(' not in body and '.Run(' not in body,
 'clear only ephemeral review state': all(x in body for x in ('markedIds.Clear();','draftStates.Clear();','liveFieldResult = null;')),
 'reload after archive': 'SafeReload();' in body and body.index('ArchiveAndClear(DateTime.UtcNow)') < body.index('SafeReload();'),
 'no Playnite metadata writes': all(x not in body for x in ('api.Database.Games.Update(', 'ApplyOne(', 'File.Delete(', 'File.WriteAllBytes(', 'new EnrichmentJournal(', 'new CatalogConfirmedIdentities(', 'new CatalogIgnored(')),
 'errors visible': 'api.Dialogs.ShowErrorMessage(' in body,
 'bounded scan preserved': 'StartDirectScan(bool allPending)' in source and 'WorkspaceScanSelection.BatchLimit' in source,
 'native batch preserved': 'WorkspaceNativeConfirmedBatch.Execute(' in source,
 'Classic fallback preserved': 'Open Classic Matcher' in source,
}
for label, ok in checks.items():
    if not ok: raise AssertionError('FAIL v0.5.50: '+label)
# Verify the safety checks are actually sensitive to missing confirmation or archive.
for pattern in ('answer != MessageBoxResult.Yes', 'cache.Load();', 'cache.ArchiveAndClear(DateTime.UtcNow)'):
    assert pattern in body and pattern not in body.replace(pattern,'',1), pattern
print('PASS:',len(checks),'native full-rescan source and preservation safeguards; Windows/Playnite unverified')
