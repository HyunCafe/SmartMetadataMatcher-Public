#!/usr/bin/env python3
"""Source invariants for native Design C cache-only IGDB refresh; no Playnite runtime certification."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
lookup=(root/'Source/WorkspaceCatalogLookup.cs').read_text(encoding='utf-8-sig')
policy=(root/'Source/WorkspaceRefreshPolicy.cs').read_text(encoding='utf-8-sig')
projects=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
testsproject=(root/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig')
runner=(root/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig')
checks={
 'explicit single saved match refresh': 'StartSavedRefresh(false)' in ui,
 'explicit marked-only saved refresh': 'StartSavedRefresh(true)' in ui,
 'selection bounded to previous 25 limit': 'WorkspaceRefreshPolicy.ChooseMarked(savedIds, markedIds)' in ui and 'WorkspaceScanSelection.BatchLimit' in policy,
 'non-saved records excluded': 'items.Where(x => x.FromSaved && x.CanMark)' in ui,
 'single target excludes invalid states': 'focused != null && focused.FromSaved && focused.CanMark' in ui,
 'no fresh network without user click': 'ActionButton("Refresh this IGDB match"' in ui and 'ActionButton("Refresh marked saved"' in ui,
 'secrets read through shared DPAPI store': 'new CatalogCredentialStore(dataPath).Load()' in ui,
 'missing credentials open settings only': 'ShowIgdbSettings();return;' in ui,
 'validate cache before lookup': 'var oldCache = new CatalogScanCache(dataPath);' in ui and 'oldCache.Load();' in ui,
 'validate live title': 'String.Equals(game.Name, latest.Name, StringComparison.Ordinal)' in ui,
 'validate live platform': 'String.Equals(live.Platform, before.Platform, StringComparison.Ordinal)' in ui,
 'validate missing fields': 'live.MissingMask != before.MissingMask' in ui,
 'validate current cached record': 'oldCache.TryGet(live, DateTime.UtcNow, out cached)' in ui,
 'same resolver/cache backend reused': 'new WorkspaceCatalogLookup(dataPath).Run(inputs,' in ui and 'CatalogWindow.ResolveWithExactFallback(input, found, client, token)' in lookup,
 'scan/refresh serialized': 'if (scanning || table == null) return;' in ui and 'scanning = true;' in ui,
 'cancel token passed to same network worker': 'scanCancellation = new CancellationTokenSource();' in ui and 'credentials.ClientId, credentials.ClientSecret, token,' in ui,
 'progress bound to dispatched UI': 'window.Dispatcher.BeginInvoke(new Action(() =>' in ui,
 'on refresh reread real cache': 'var current = new CatalogScanCache(dataPath);' in ui and 'current.Load();' in ui,
 'changed identity unmarks before reload': 'markedIds.Remove(input.Id)' in ui and 'WorkspaceRefreshPolicy.SameIdentity(previous[input.Id].CatalogId,' in ui,
 'reloaded library uses cache': 'SafeReload();' in ui and 'scanCache.TryGet(input, now, out cached)' in ui,
 'errors and cancellations surfaced': 'result.Error' in ui and 'result.Canceled' in ui and 'Scan canceled' in ui,
 'explicit never approval text': 'Live lookup is not import approval.' in ui and 'identity not newly confirmed' in ui.lower(),
 'score not probability': 'ranking score ' in ui and '(not a probability)' in ui,
 'saved evidence bounded': 'evidence.Length > 380' in ui and 'evidence.Substring(0, 380)' in ui,
 'saved IGDB ID displayed': 'IGDB ID " + row.CachedCatalogId' in ui,
 'saved lookup UTC date displayed': 'row.CachedScannedUtc' in ui,
 'cannot refresh completed or ignored': 'row.FromSaved && row.CanMark' in ui,
 'bulk-mark updates refresh controls': 'finally {bulkMarking = false; UpdateSelectionSummary(); UpdateScanControls();}' in ui,
 'refresh button count does not shadow scan count': 'int savedMarkedCount = items.Count(' in ui and 'refreshMarkedButton.IsEnabled = !scanning && savedMarkedCount > 0;' in ui and 'Math.Min(savedMarkedCount, WorkspaceScanSelection.BatchLimit)' in ui,
 'clear marks updates refresh controls': 'markedIds.Clear();\n            UpdateSelectionSummary(); UpdateScanControls();' in ui,
 'no live fetch on inspector change': 'private void UpdateInspector()' in ui and 'new IgdbReadOnly(' not in ui,
 'no metadata writer in UI': all(x not in ui for x in ('Database.Games.Update(', '.SaveBatch(', 'RunBatch(', '.Commit(', 'File.WriteAllBytes(')),
 'no metadata writer in lookup': all(x not in lookup for x in ('Database.Games.Update(', 'new EnrichmentJournal(', 'new MetadataJournal(', 'RunBatch(', '.ArchiveAndClear(')),
 'policy rejects invalid IGDB ids': 'before > 0' in policy and 'after == before' in policy,
 'policy exact title equality': 'StringComparison.Ordinal' in policy,
 'policy removes empty and duplicate ids': '.Distinct().Take(' in policy and 'Guid.Empty' in policy,
 'test project includes new policy and tests': 'WorkspaceRefreshPolicyTests.cs' in testsproject and 'WorkspaceRefreshPolicy.cs' in testsproject,
 'plugin project includes new policy': 'Source/WorkspaceRefreshPolicy.cs' in projects,
 'offline policy tests wired': 'WorkspaceRefreshPolicyTests.Run();' in runner,
 'original selected-review handoff retained': 'classic.StageVerifiedNativeReview(verifiedReview);' in ui,
 'original Classic directly accessible': 'new CatalogWindow(api, dataPath).Show();' in ui,
}
failed=[k for k,v in checks.items() if not v]
for f in failed:print('FAIL:',f)
if failed:raise SystemExit(1)
print('PASS:',len(checks),'native read-only refresh/identity-change source checks; Windows and Playnite unverified')
