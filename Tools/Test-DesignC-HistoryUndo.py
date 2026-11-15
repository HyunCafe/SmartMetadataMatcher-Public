#!/usr/bin/env python3
"""v0.5.53 native history and safe latest-undo static contract."""
from pathlib import Path
import re
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8')
code=(root/'Source/WorkspaceHistoryUndo.cs').read_text(encoding='utf-8')
proj=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8')
assertions={
  'latest and selected use one existing writer':code.count('CatalogEnrichmentWriter.UndoLast(')==2 and 'CatalogEnrichmentWriter.UndoLast(api,journal);' in code,
  'no duplicate journal implementation':'class EnrichmentJournal' not in code and 'journal.Prepare(' not in code,
  'fresh journal read after consent':'var preview = PreviewLatest(dataPath);' in code,
  'operation identity invalidation':'preview.OperationId != approvedOperationId' in code,
  'two-ledger recovery guard':'CatalogRecoveryConsistency.Inspect(' in code and 'journal.MediaStaging.AllStages()' in code and 'journal.AllEntries()' in code,
  'blocks uncommitted and undone':'!latest.Committed || latest.Undone' in code,
  'pre-undo game existence':'api.Database.Games[preview.GameId]' in code,
  'no provider or separate importer':'new IgdbReadOnly(' not in code and 'CatalogEnrichmentWriter.ApplyOne(' not in code,
  'explicit user approval': 'if (confirm.ShowDialog()!=true) return;' in ui,
  'backup acknowledgement':'backup.Checked+=(s,e)=>undo.IsEnabled=true;' in ui and 'backup.Unchecked+=(s,e)=>undo.IsEnabled=false;' in ui,
  'undo initially disabled':'undo.IsEnabled=false;' in ui,
  'approval bound to latest ID':'WorkspaceHistoryUndo.UndoLatest(api,dataPath,latest.OperationId)' in ui,
  'UI no direct writer':'CatalogEnrichmentWriter.UndoLast(' not in ui,
  'UI no recovery writes':'journal.MarkUndone(' not in ui,
  'active operations excluded':'scanning || checkingLiveFields || checkingBatchFields || nativeApplying' in ui,
  'clear legacy fallback':'legacy v0.4.0 operations use Classic Matcher' in ui,
  'read-only recovery status':'InspectRecovery(dataPath)' in ui,
  'history stays visible':'private void ShowHistoryPreview()' in ui and 'AllEntries()' in ui,
  'history retains latest and adds strict selected prefix':'Undo latest import...' in ui and 'Undo selected recent...' in ui and 'EnrichmentPlan.IsLatestUndoPrefix(' in code,
  'build includes new service':proj.count('Compile Include="Source/WorkspaceHistoryUndo.cs"')==1,
}
for label,ok in assertions.items():
 if not ok:raise AssertionError('FAIL: '+label)
print('PASS:',len(assertions),'native latest-undo, recovery, approval and single-writer safeguards; Windows/Playnite runtime unverified')
