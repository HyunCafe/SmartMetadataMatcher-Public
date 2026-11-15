#!/usr/bin/env python3
"""v0.5.55: Design C selected recent undo must share the existing journal/writer."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
service=(root/'Source/WorkspaceHistoryUndo.cs').read_text(encoding='utf-8-sig')
policy=(root/'Source/EnrichmentPlan.cs').read_text(encoding='utf-8-sig')
writer=(root/'Source/CatalogEnrichmentWriter.cs').read_text(encoding='utf-8-sig')
test=(root/'Tools/Test-DesignC-HistoryUndo.py').read_text(encoding='utf-8-sig')
checks={
 'selected UI entry point':'Undo selected recent...' in ui and 'UndoSelectedFromHistory(Window historyDialog, DataGrid historyGrid)' in ui,
 'latest-only UI stays':'Undo latest import...' in ui and 'UndoLatestFromHistory(dialog)' in ui,
 'identity from actual journal':'OperationId = x.OperationId' in ui and 'public Guid OperationId {get; set;}' in ui,
 'selected frozen before confirmation':'var chosen = historyGrid.SelectedItems.OfType<WorkspaceHistoryPreviewRow>().ToList();' in ui,
 'no empty selection':'if (chosen.Count == 0)' in ui,
 'reject inactive UI rows':'x.OperationId == Guid.Empty || x.Status != "Recorded applied"' in ui,
 'previews from service':'WorkspaceHistoryUndo.PreviewSelected(dataPath,' in ui,
 'strict journal prefix':'EnrichmentPlan.IsLatestUndoPrefix(active.Select(x => x.OperationId),requested)' in service,
 'policy rejects duplicates':'selected.Distinct().Count()!=selected.Count' in policy,
 'policy rejects gaps':'history.Take(selected.Count)' in policy,
 'policy ignores sort order':'selected.OrderBy(id=>id)' in policy,
 'committed only':'var active = journal.Committed();' in service,
 'recovery before preview':'CheckRecovery(journal);' in service,
 'recovery crosses BOTH logs':'journal.MediaStaging.AllStages(), journal.AllEntries()' in service,
 'no stale approval':'var preview = PreviewSelected(dataPath,approved);' in service,
 'compare each operation':'latest.OperationId != expected.OperationId' in service,
 'per-operation fresh ledger check':'var latest = PreviewLatest(dataPath);' in service,
 'live game check':'api.Database.Games[expected.GameId] == null' in service,
 'one shared writer':'CatalogEnrichmentWriter.UndoLast(api,journal);' in service and service.count('CatalogEnrichmentWriter.UndoLast(')==2,
 'writer blocks manually changed fields':'EnrichmentPlan.CanUndo(' in writer and 'ExtraFieldsUndoSafe' in writer,
 'no direct database writer in workspace':'CatalogEnrichmentWriter.UndoLast(' not in ui and '.MarkUndone(' not in ui,
 'approval itemized':'var list = String.Join("\\n\\n", approved.Select(' in ui and 'x.Fields' in ui,
 'approval scrollable':'new ScrollViewer {VerticalScrollBarVisibility = ScrollBarVisibility.Auto}' in ui,
 'backup default off':'undoSelected.IsEnabled = false;' in ui and 'IsChecked = false' in ui,
 'backup requirement':'backup.Checked += (s,e) => undoSelected.IsEnabled=true;' in ui and 'backup.Unchecked += (s,e) => undoSelected.IsEnabled=false;' in ui and 'backup.IsChecked!=true' in ui,
 'cancel is nonwriting':'confirm.DialogResult=false' in ui and 'confirm.ShowDialog()!=true' in ui,
 'UI prevents competing work':'if (scanning || checkingLiveFields || checkingBatchFields || nativeApplying)' in ui,
 'IDs frozen for writer':'var ids = approved.Select(x => x.OperationId).ToList();' in ui and 'WorkspaceHistoryUndo.UndoSelected(api,dataPath,ids)' in ui,
 'fail-fast':'catch (Exception ex)' in service and 'Stopped after ' in service and 'completed++' in service,
 'partial state UI refresh':'historyDialog.Close();' in ui and 'SafeReload();' in ui,
 'old history source test migrated':'history retains latest and adds strict selected prefix' in test,
 'no new journal, no file delete':'class EnrichmentJournal' not in service and 'File.Delete(' not in service,
 'legacy fallback retained':'legacy v0.4.0 operations use Classic Matcher' in ui,
}
for k,v in checks.items():
 if not v:raise AssertionError('FAIL: '+k)
print('PASS:',len(checks),'selected newest-first undo safeguards; runtime write/undo unverified')
