#!/usr/bin/env python3
"""Design C native ignore/restore workflow source-invariants; run on complete checkout."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf8')
service=(root/'Source/WorkspaceIgnoreDecision.cs').read_text(encoding='utf8')
store=(root/'Source/CatalogIgnored.cs').read_text(encoding='utf8')
proj=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf8')
tproj=(root/'Tests/NameRulesTests.csproj').read_text(encoding='utf8')
test=(root/'Tests/WorkspaceIgnoreDecisionTests.cs').read_text(encoding='utf8')
tmain=(root/'Tests/NameRulesTests.cs').read_text(encoding='utf8')
checks={
 'existing store reused': service.count('new CatalogIgnored(dataPath)') == 2 and 'SetMany(string dataPath' in service and 'internal static void Set(string dataPath' in service and 'class CatalogIgnored' in store,
 'no duplicate persistence format': 'catalog-ignored-ids.txt' not in service and 'File.Write' not in service,
 'single store transition': service.count('store.Save(revised)') == 1 and 'store.Load()' in service,
 'stale state refused': 'before.Contains(gameId) != expectedIgnored' in service,
 'invalid no-op refused': 'gameId == Guid.Empty || expectedIgnored == ignore' in service,
 'durable reread verified': 'saved.SetEquals(revised)' in service,
 'no Playnite metadata writes': all(x not in service for x in ('Database.Games.Update(', 'CatalogEnrichmentWriter.ApplyOne(', 'CatalogEnrichmentWriter.UndoLast(', 'new EnrichmentJournal(', 'IgdbReadOnly')),
 'UI does not mutate stored preferences': all(x not in ui for x in ('new CatalogIgnored(dataPath).Save(', 'store.Save(', '.SaveBatch(')),
 'single service call': ui.count('WorkspaceIgnoreDecision.Set(dataPath, selected.GameId, ignored, !ignored)') == 1,
 'busy operation blocked': 'if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying) return;' in ui,
 'updated and recovery prevented': 'selected.Group == WorkspaceTaskGroup.Updated' in ui and 'row.Group != WorkspaceTaskGroup.NeedsRecovery' in ui,
 'game existence/name rechecked': 'api.Database.Games[selected.GameId] == null' in ui and 'selected.Title, StringComparison.Ordinal' in ui,
 'explicit YesNo': 'verb + " game", MessageBoxButton.YesNo, MessageBoxImage.Question' in ui,
 'restore is reversible': 'Restore this game' in ui and 'Ignore this game' in ui and 'ToggleFocusedIgnore' in ui,
 'selection/draft invalidated': all(x in ui for x in ('markedIds.Remove(selected.GameId);','draftStates.Remove(selected.GameId);','freshStrictEvidence.Remove(selected.GameId);','liveFieldResult = null;')),
 'refresh/preserve focus': 'var revised = items.FirstOrDefault(x => x.GameId == selected.GameId);' in ui and 'table.ScrollIntoView(revised);' in ui,
 'no new provider inside UI': 'new IgdbReadOnly(' not in ui,
 'restored status from shared classifier': 'WorkspaceTaskClassifier.Classify(state)' in ui and 'new CatalogIgnored(dataPath).Load()' in ui,
 'single compilation entry': proj.count('Compile Include="Source/WorkspaceIgnoreDecision.cs"') == 1,
 'test compilation': tproj.count('Compile Include="../Source/WorkspaceIgnoreDecision.cs"')==1 and tproj.count('Compile Include="WorkspaceIgnoreDecisionTests.cs"')==1,
 'unit tests invoked': 'WorkspaceIgnoreDecisionTests.Run();' in tmain and 'MustRefuse(' in test,
 'classic remains': 'private void OpenClassic()' in ui and 'Open Classic Matcher' in ui,
}
failed=[x for x,ok in checks.items() if not ok]
if failed: raise SystemExit('FAIL: '+', '.join(failed))
print('PASS:',len(checks),'native ignore/restore contract checks; Playnite runtime unverified')
