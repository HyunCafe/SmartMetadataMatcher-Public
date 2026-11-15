#!/usr/bin/env python3
"""v0.5.49 native full pending scan source invariant checks (no Playnite writes)."""
from pathlib import Path
s=(Path(__file__).resolve().parent.parent/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
start=s.split('private void StartDirectScan(bool allPending)',1)
assert len(start)==2,'Missing explicit all-pending scan mode'
body=start[1].split('private void StartSavedRefresh(',1)[0]
checks={
 'keeps original bounded entry point':'private void StartDirectScan()' in s and 'StartDirectScan(false);' in s,
 'explicit all pending button':'ActionButton("Scan all pending"' in s and 'StartDirectScan(true)' in s,
 'does not use huge one-shot lookups':'new WorkspaceCatalogLookup(dataPath).Run(chunk,' in body and 'Run(inputs,' not in body,
 '25-game batches':'offset += WorkspaceScanSelection.BatchLimit' in body and 'Take(WorkspaceScanSelection.BatchLimit)' in body,
 'selected-only semantics': 'markedIds.Count == 0 ? pending :' in body and 'pending.Where(x => markedIds.Contains(x)).ToList()' in body,
 'existing bounded semantics':'WorkspaceScanSelection.Choose(pending, markedIds)' in body,
 'fail-closed selected count':'inputs.Count != selected.Count' in body,
 'progress reflects complete queue':'earlier + done' in body and '" / " + inputs.Count' in body,
 'cancel between requests':'token.ThrowIfCancellationRequested();' in body,
 'stop at failed request':'if (!String.IsNullOrWhiteSpace(result.Error))' in body and 'failure = result.Error; break;' in body,
 'stop at partial result':'result.Completed != chunk.Count' in body,
 'keep earlier results on cancel':'catch (OperationCanceledException) {canceled = true;}' in body and 'Completed results retained.' in body,
 'one safe reload':'SafeReload();' in body and 'UpdateScanControls();' in body,
 'no direct writes':'api.Database.Games.Update(' not in body and 'CatalogEnrichmentWriter.ApplyOne(' not in body,
 'all button disabled during operations':'scanAllPendingButton.IsEnabled = !scanning && !checkingLiveFields && available > 0' in s,
 'old scan still exists':'scanPendingButton = ActionButton("Scan next 25 pending"' in s,
 'no unattended import':'WorkspaceNativeConfirmedBatch.Execute(' not in body and 'WorkspaceImportCoordinator.ApplyOne(' not in body,
}
for label,passed in checks.items():
 if not passed:raise AssertionError('FAIL v0.5.49 full pending scan: '+label)
# Mutation-style negative sanity check: these gates must be sensitive to omissions.
for fragment in ('token.ThrowIfCancellationRequested();',
                 'Take(WorkspaceScanSelection.BatchLimit)',
                 'inputs.Count != selected.Count',
                 'failure = result.Error; break;'):
 assert fragment in body and fragment not in body.replace(fragment,'',1),fragment
print('PASS:',len(checks),'read-only Scan All Pending source gates; Windows/Playnite runtime unverified')
