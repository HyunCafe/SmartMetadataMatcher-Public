#!/usr/bin/env python3
"""v0.5.60: History visibility, existing diagnostics and native context review safety."""
from pathlib import Path
import re
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8')
visibility=(root/'Source/HistoryVisibilityStore.cs').read_text(encoding='utf-8-sig')
export=(root/'Source/DiagnosticExport.cs').read_text(encoding='utf-8-sig')
writer=(root/'Source/WorkspaceHistoryUndo.cs').read_text(encoding='utf-8-sig')
classic=(root/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
# Version alignment is a live contract, not pinned to the version when this test was first written.
manifest_version = re.search(r"(?m)^Version: (\d+\.\d+\.\d+)\s*$",
    (root/'extension.yaml').read_text())
plugin_source = (root/'Source/Plugin.cs').read_text()
version_aligned = False
if manifest_version:
    v = manifest_version.group(1)
    version_aligned = tuple(map(int, v.split('.'))) >= (0, 5, 60) and all(
        f'{attribute}("{v}.0")' in plugin_source for attribute in ('AssemblyVersion', 'AssemblyFileVersion'))
checks={
 'shared history visibility store': 'new HistoryVisibilityStore(dataPath)' in ui and 'class WorkspaceHistoryVisibility' in visibility and 'new HistoryVisibilityStore(dataPath)' in visibility,
 'legacy store and format retained': 'history-hidden-operations.txt' in visibility and 'HistoryVisibilityStore' in classic,
 'native history status filter': 'HistoryViewFilter.All,HistoryViewFilter.Applied,HistoryViewFilter.Undone' in ui,
 'native history search': 'historySearch.TextChanged +=' in ui and 'HistoryViewFilter.Includes(' in ui,
 'native include hidden toggle': 'Content="Include hidden"' in ui and 'includeHidden.Checked +=' in ui and 'includeHidden.Unchecked +=' in ui,
 'native hide/restore controls': 'Hide selected from this view...' in ui and 'Restore selected hidden operations...' in ui and 'ChangeHistoryVisibility(grid,true,refreshHistoryView)' in ui and 'ChangeHistoryVisibility(grid,false,refreshHistoryView)' in ui,
 'native history shows state': 'VisibilityLabel' in ui and 'AddColumn(grid, "Visibility", "VisibilityLabel", 100)' in ui,
 'default hides only presentation': 'includeHidden.IsChecked==true || !x.Hidden' in ui,
 'no journal mutation for history visibility': 'MarkUndone(' not in visibility and 'journal.Commit(' not in visibility and 'api.Database.' not in visibility and 'RemoveFile(' not in visibility,
 'visibility rejects missing IDs': 'ids.Any(id=>!journalIds.Contains(id))' in visibility,
 'visibility rejects no-op and races': 'before.Contains(id)==hide' in visibility and 'ids.Distinct().Count()!=ids.Count' in visibility,
 'history visibility atomic reuse': 'store.Save(updated)' in visibility and 'store.Load().SetEquals(updated)' in visibility and 'File.Replace(temp,path,null)' in visibility,
 'visibility action requires explicit hide approval': 'Hide history entries",MessageBoxButton.YesNo' in ui,
 'UI no direct visibility write': 'store.Save(' not in ui and 'new EnrichmentJournal(dataPath)' not in ui.split('private void ChangeHistoryVisibility(')[1].split('private void ShowHistoryPreview()')[0],
 'native recovery and selected undo intact': 'WorkspaceHistoryUndo.UndoSelected(api,dataPath,ids)' in ui and 'WorkspaceHistoryUndo.UndoLatest(api,dataPath,latest.OperationId)' in ui and 'EnrichmentPlan.IsLatestUndoPrefix(' in writer,
 'diagnostic exporter accessible': 'Export library diagnostics (CSV, read-only)...' in ui and 'DiagnosticExport.Run(api)' in ui,
 'uses original complete-library diagnostics': 'api.Database.Games.Where(g => g != null).ToList()' in export and 'DiagnosticCsv.Serialize(records)' in export,
 'CSV requires SaveFileDialog': 'new SaveFileDialog' in export and 'save.ShowDialog() != true' in export and 'OverwritePrompt = true' in export,
 'CSV does not write game records': 'Games.Update(' not in export and 'RemoveFile(' not in export,
 'context menu anchors retained': 'Mark highlighted in preview' in ui and 'Copy focused game title' in ui and 'Unmark highlighted' in ui,
 'context actions use existing routes': all(x in ui for x in ['checkFields.Click += (sender,args) => CheckLiveFieldAvailability();','chooseIdentity.Click += (sender,args) => ShowIdentitySearch();','editProposal.Click += (sender,args) => EditFocusedDraft();','ignoreMenu.Click += (sender,args) => ToggleFocusedIgnore();','openHistory.Click += (sender,args) => ShowHistoryPreview();']),
 'context enables only valid focus and idle': 'context.Opened +=' in ui and 'bool idle=!scanning && !checkingLiveFields && !checkingBatchFields && !nativeApplying;' in ui,
 'context targets clicked row': 'table.PreviewMouseRightButtonDown +=' in ui and 'table.SelectedItem=target==null ? null : target.Item;' in ui,
 'context clipboard only saved positive ID': 'Int64.TryParse(item.CachedCatalogId,out candidateId) && candidateId>0' in ui,
 'existing safe title cleanup still available': 'SuggestedUniqueDisplayTitle' in ui or 'CatalogEnrichmentWriter' not in ui,
 'no second importer or direct metadata writer': all(x not in ui for x in ('api.Database.Games.Update(', 'CatalogEnrichmentWriter.UndoLast(', 'CatalogEnrichmentWriter.ApplyOne(', 'new IgdbReadOnly(')),
 'native ignored-game service intact': 'WorkspaceIgnoreDecision.Set(dataPath, selected.GameId, ignored, !ignored)' in ui,
 'classic retained': 'Open Classic Matcher' in ui and 'private void OpenClassic()' in ui,
 'version consistent': version_aligned,
}
failed=[name for name,ok in checks.items() if not ok]
if failed: raise SystemExit('FAIL: '+', '.join(failed))
print('PASS:',len(checks),'native History visibility, diagnostics, contextual review and migration/undo safeguards. Windows/Playnite runtime not verified.')
