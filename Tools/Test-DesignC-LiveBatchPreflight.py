#!/usr/bin/env python3
"""Native selected exact-field inspection: source-level write/snapshot guard checks."""
from pathlib import Path
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
plan=(r/'Source/WorkspaceBatchReview.cs').read_text(encoding='utf-8-sig')
tests=(r/'Tests/WorkspaceBatchReviewTests.cs').read_text(encoding='utf-8-sig')
checks=0
def require(ok,label):
 global checks
 checks+=1
 if not ok:raise AssertionError('v0.5.39 live-batch check: '+label)
for code in (
 'private void CheckMarkedLiveFields(',
 'var target = WorkspaceBatchLivePreflight.Capture(input,catalogId,',
 'ConfirmUnchangedLookup(input,item.CachedCatalogId,item.CachedCatalogName);',
 'WorkspaceBatchLivePreflight.EnsureCurrent(entry.Item2,current,',
 'WorkspaceLiveComparison.Build(current,result,',
 'WorkspaceDraftPolicy.CheckComparison(current,compared);',
 'WorkspaceDraftPolicy.Included(entry.Item2.Draft,f.Field)',
 'review.FetchExact(entry.Item2.IgdbId,',
 'dialog.Dispatcher.Invoke(new Action(() => {',
 'if (batchFieldCancellation != null) batchFieldCancellation.Cancel();',
 'batchFieldCancellation = cancellation;',
 'checkingBatchFields = true;',
 'continueButton.IsEnabled = false;',
 'continueButton.IsEnabled = WorkspaceBatchLivePreflight.DetailedPlanReady(',
 'dialog.Closing += (sender, args) => {',
 'AddColumn(grid, "Field check", "LiveCheck", 155);',
 'ShowVerifiedBatchDetails(dialog,plan,verifiedTargets,verifiedDetails)',
 'entry.Item3.AvailableFields = fields.Count == 0 ?',
 'No import approved or applied.',
 'classic.StageVerifiedNativeReview(verifiedReview);',
 'classic.StageTransferredDrafts(transferDrafts);',
): require(code in ui,'UI contract: '+code)
for code in (
 'internal sealed class WorkspaceLiveBatchTarget',
 'internal static class WorkspaceBatchLivePreflight',
 'internal static WorkspaceLiveBatchTarget Capture(',
 'internal static void EnsureCurrent(',
 'String.Equals(current.Name,expected.GameName,StringComparison.Ordinal)',
 'current.MissingMask != expected.MissingMask',
 'current.LocalReleaseYear != expected.LocalReleaseYear',
 'draft == null ? null : draft.Copy()',
 'StrictEvidence = x.StrictEvidence',
 'plannedNames[index] = Tuple.Create(current.GameId,title,current.Platform);',
):require(code in plan,'snapshot and planner: '+code)
for code in (
 'changed exact IGDB ID rejected',
 'Playnite title changed during network request rejected',
 'supported missing metadata changed during network request rejected',
 'invalid IGDB identity cannot start preflight',
):require(code in tests,'C# regression: '+code)
for dangerous in ('CatalogEnrichmentWriter.ApplyOne(', 'Database.Games.Update(',
                  'journal.Prepare(', 'journal.Commit(', 'MediaStaging.Begin('):
 require(dangerous not in ui and dangerous not in plan,
         'No second metadata-write path: '+dangerous)
import re
manifest = (r/'extension.yaml').read_text(encoding='utf-8-sig')
plugin = (r/'Source/Plugin.cs').read_text(encoding='utf-8-sig')
match = re.search(r'(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$', manifest)
require(match is not None and tuple(map(int, match.group(1).split('.'))) >= (0,5,39),
        'Manifest version, v0.5.39 or newer')
require('AssemblyVersion("'+match.group(1)+'.0")' in plugin,
        'Assembly version matches manifest')
print('PASS:',checks,'v0.5.39 native read-only live-batch invariants; Windows/Playnite unverified')
