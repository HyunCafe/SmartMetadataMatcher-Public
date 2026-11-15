#!/usr/bin/env python3
"""v0.5.60: explicit selected bulk ignore/restore and scoped mark actions."""
from pathlib import Path
import re
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf8')
service=(r/'Source/WorkspaceIgnoreDecision.cs').read_text(encoding='utf8')
test=(r/'Tests/WorkspaceIgnoreDecisionTests.cs').read_text(encoding='utf8')
store=(r/'Source/CatalogIgnored.cs').read_text(encoding='utf8')
# Version alignment is a live contract, not pinned to the version when this test was first written.
manifest_version = re.search(r"(?m)^Version: (\d+\.\d+\.\d+)\s*$",
    (r/'extension.yaml').read_text())
plugin_source = (r/'Source/Plugin.cs').read_text()
version_aligned = False
if manifest_version:
    v = manifest_version.group(1)
    version_aligned = tuple(map(int, v.split('.'))) >= (0, 5, 60) and all(
        f'{attribute}("{v}.0")' in plugin_source for attribute in ('AssemblyVersion', 'AssemblyFileVersion'))
checks={
 'four actions, compact menu': all(x in ui for x in ('Bulk actions','Mark all shown eligible games','Unmark shown games','Ignore highlighted games...','Restore highlighted ignored games...')) and 'new ContextMenu {PlacementTarget=bulkActionsButton' in ui,
 'ignored status quick view': 'private void ShowIgnoredGames()' in ui and 'Show ignored games (' in ui and 'onlyIgnoredView = true;' in ui,
 'exact ignored filter': '(onlyIgnoredView && row.Group != WorkspaceTaskGroup.Ignored)' in ui,
 'filter can be reset': 'onlyIgnoredView = false;' in ui and '|| onlyIgnoredView;' in ui,
 'ignored filter is not persistent': 'private bool onlyIgnoredView;' in ui and 'CatalogIgnored(dataPath)' not in ui.split('private void ShowIgnoredGames()')[1].split('private void ClearMarks()')[0],
 'view-local marking': 'table.Items.Cast<object>().OfType<LibraryWorkspaceItem>()' in ui and 'Where(x => x.CanMark && x.Marked != mark)' in ui,
 'marks never authorize import': 'MarkShown(bool mark)' in ui and 'marked for review only. No metadata changed.' not in ui and 'No metadata changed.' in ui,
 'marks retained outside view': 'Unmark shown games' in ui and 'markedIds.Clear();' in ui,
 'bulk action blocked while busy': 'private void SetHighlightedIgnored(bool ignore)' in ui and 'if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying ||' in ui,
 'reject mixed selection': 'selected.Any(x => ignore ?' in ui and 'x.Group != WorkspaceTaskGroup.Ignored' in ui and 'x.Group == WorkspaceTaskGroup.Updated' in ui,
 'no identity guess': 'selected.Select(x => x.GameId).Distinct().Count() != selected.Count' in ui,
 'live game state check': 'api.Database.Games[row.GameId]' in ui and 'ScanInput(live).Platform,row.Platform' in ui,
 'explicit complete batch approval': 'verb + " highlighted games",MessageBoxButton.YesNo' in ui and 'selected.Take(10)' in ui,
 'service used exactly once in UI': ui.count('WorkspaceIgnoreDecision.SetMany(dataPath,')==1,
 'one existing preference store': 'new CatalogIgnored(dataPath)' in service and 'catalog-ignored-ids.txt' in store,
 'service rejects malformed and races': 'before.Contains(id) != expectedIgnored' in service and 'ids.Distinct().Count() != ids.Count' in service,
 'one atomic batch write': service.count('store.Save(next)')==1 and 'store.Load().SetEquals(next)' in service,
 'no new metadata writer': all(x not in service for x in ('api.Database.Games.Update(','new EnrichmentJournal(','CatalogEnrichmentWriter','IgdbReadOnly','File.WriteAllText(')),
 'UI no new metadata writer': all(x not in ui for x in ('api.Database.Games.Update(', 'CatalogEnrichmentWriter.ApplyOne(', 'CatalogEnrichmentWriter.UndoLast(', 'new IgdbReadOnly(')),
 'state invalidation after success': all(x in ui for x in ('markedIds.Remove(row.GameId);','draftStates.Remove(row.GameId);','freshStrictEvidence.Remove(row.GameId);','liveFieldResult=null;')),
 'failed bulk has no deliberate partial update': 'Mixed saved ignored state must reject entire batch.' in test and 'Rejected mixed batch made partial changes.' in test,
 'unit tests invoke SetMany': test.count('WorkspaceIgnoreDecision.SetMany(')>=8,
 'existing single ignore preserved': 'WorkspaceIgnoreDecision.Set(dataPath, selected.GameId, ignored, !ignored)' in ui,
 'old undo and import intact': 'WorkspaceHistoryUndo.UndoSelected(api,dataPath,ids)' in ui and 'WorkspaceNativeConfirmedBatch' in ui,
 'build version updated': version_aligned,
 'documented new mode': all(x in (r/'CHANGELOG.md').read_text() for x in ('## 0.5.57', 'Mark shown / Unmark shown', 'one atomic', 'Bulk actions menu')),
}
failed=[k for k,v in checks.items() if not v]
if failed:raise SystemExit('FAIL: '+', '.join(failed))
print('PASS:',len(checks),'bulk triage safety/selection checks; Windows/Playnite unverified')
