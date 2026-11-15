#!/usr/bin/env python3
"""Static integration gate: native Design C scanning must never import metadata."""
from pathlib import Path
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
lookup=(r/'Source/WorkspaceCatalogLookup.cs').read_text(encoding='utf-8-sig')
selection=(r/'Source/WorkspaceScanSelection.cs').read_text(encoding='utf-8-sig')
classic=(r/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
proj=(r/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
tproj=(r/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig')
runner=(r/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig')
tests=(r/'Tests/WorkspaceScanSelectionTests.cs').read_text(encoding='utf-8-sig')
checks=0
def check(ok,detail):
 global checks
 checks+=1
 if not ok:raise AssertionError('FAIL: '+detail)
for marker in ('ShowIgdbSettings()', 'new CatalogCredentialStore(dataPath).Save(clientId.Text, clientSecret.Password)',
  'new CatalogCredentialStore(dataPath).Load()',
  'WorkspaceCatalogLookup.TestConnection(id, secret, testToken.Token)',
  'scanCancellation.Cancel()', 'if (scanning) return;',
  'WorkspaceScanSelection.Choose(pending, markedIds)',
  'live.MissingCount <= 0 || live.MissingMask != prior.MissingMask',
  'Task.Run(() => {', 'new WorkspaceCatalogLookup(dataPath).Run(inputs,',
  'SafeReload();', 'UpdateScanControls();',
  'scanPendingButton.IsEnabled = !scanning && available > 0;',
  'classic.StageVerifiedNativeReview(verifiedReview);'):
 check(marker in ui,'Design C UI retains '+marker)
for banned in ('new IgdbReadOnly(', '.SaveBatch(', 'Database.Games.Update(',
               '.Commit(', 'File.WriteAllText(', 'File.WriteAllBytes(', 'RunBatch('):
 check(banned not in ui,'UI must not become second importer: '+banned)
for marker in ('new CatalogScanCache(dataPath)', 'cache.Load();', 'new CatalogScanCheckpoint(cache)',
 'CatalogCore.QueryVariants(input.Name, input.Platform)',
 'CatalogWindow.ResolveWithExactFallback(input, found, client, token)',
 'client.SearchVariants(queries.Select(q => q.Text), token)',
 'checkpoints.Add(record, DateTime.UtcNow)',
 'checkpoints.Flush(DateTime.UtcNow)',
 'catch (OperationCanceledException) { outcome.Canceled = true; }',
 'new IgdbReadOnly(clientId, clientSecret)',
 'client.TestConnection(token)'):
 check(marker in lookup,'shared lookup '+marker)
for banned in ('Database.Games.Update(', 'new MetadataJournal(', 'new EnrichmentJournal(',
               'RunBatch(', 'CatalogCredentialStore', 'File.WriteAllText(', '.ArchiveAndClear('):
 check(banned not in lookup,'lookup does not write Playnite/credentials: '+banned)
check('internal static CatalogMatch ResolveWithExactFallback(' in classic,
      'classic existing resolver exposed without altered algorithm')
check('internal const int BatchLimit = 25;' in selection, 'strict bounded batch')
check('marks.Count == 0 || marks.Contains(id)' in selection, 'marked-only scope')
check('pending.Where(id => id != Guid.Empty && seen.Add(id)' in selection,'deduplicate queue')
check('WorkspaceScanSelectionTests.Run();' in runner,'offline test invoked')
check('WorkspaceScanSelectionTests.cs' in tproj,'offline test compiled')
check('WorkspaceScanSelection.cs' in tproj,'pure selection linked to tests')
check('Source/WorkspaceScanSelection.cs' in proj,'selection in plugin')
check('Source/WorkspaceCatalogLookup.cs' in proj,'native lookup in plugin')
check('7 Design C native scan-selection policy checks' in tests,'selection cases present')
print('PASS:',checks,'native Design C settings and scan source safeguards; Windows build still required')
