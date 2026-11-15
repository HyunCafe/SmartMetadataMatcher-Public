#!/usr/bin/env python3
"""v0.5.40 read-only verified handoff regression; no Playnite SDK writes."""
from pathlib import Path
import re
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text('utf-8-sig')
plan=(r/'Source/WorkspaceBatchReview.cs').read_text('utf-8-sig')
tests=(r/'Tests/WorkspaceBatchReviewTests.cs').read_text('utf-8-sig')
prior=(r/'Tools/Test-DesignC-LiveBatchPreflight.py').read_text('utf-8-sig')
manifest=(r/'extension.yaml').read_text('utf-8-sig')
plugin=(r/'Source/Plugin.cs').read_text('utf-8-sig')
checks=0
def require(ok,label):
 global checks
 checks+=1
 if not ok: raise AssertionError('v0.5.40 gate: '+label)
for anchor in (
 'var verifiedTargets = new Dictionary<Guid, WorkspaceLiveBatchTarget>();',
 'continueButton.IsEnabled = false;',
 'WorkspaceBatchLivePreflight.DetailedPlanReady(plan, verifiedTargets,',
 'CheckMarkedLiveFields(chosen, plan, grid, dialog, liveStatus,',
 'continueButton, liveCheckButton, detailsButton,',
 'Dictionary<Guid,WorkspaceLiveBatchTarget> verifiedTargets,',
 'verifiedTargets.Clear();',
 'verifiedTargets[entry.Item1.GameId] = entry.Item2;',
 'verifiedTargets.Remove(entry.Item1.GameId);',
 'WorkspaceBatchLivePreflight.EnsureCurrent(checkedGame,current,',
 'WorkspaceBatchLivePreflight.SameDraft(checkedGame.Draft,',
 'ConfirmUnchangedLookup(current,item.CachedCatalogId,item.CachedCatalogName);',
 'continueButton.IsEnabled = WorkspaceBatchLivePreflight.DetailedPlanReady(',
 'classic.StageTransferredDrafts(transferDrafts);',
 'classic.StageVerifiedNativeReview(verifiedReview);',
): require(anchor in ui,'native gated transfer: '+anchor)
for anchor in (
 'internal static bool ReadyForHandoff(',
 'internal static bool SameDraft(',
 'x.LiveCheck == "Fields checked · not approved"',
 'verified[x.GameId].GameId == x.GameId',
 'first.CustomTitleEnabled == second.CustomTitleEnabled',
 'first.CustomDescriptionEnabled == second.CustomDescriptionEnabled',
 'first.Description == second.Description',
 'first.Genres == second.Genres',
 'first.Series == second.Series',
 'StrictEvidence = x.StrictEvidence',
 'plannedNames[index] = Tuple.Create(current.GameId,title,current.Platform);',
): require(anchor in plan,'pure transfer gate: '+anchor)
for anchor in (
 'unverified selected game cannot leave native review',
 'failed field check blocks transfer',
 'unsafe proposed title blocks transfer',
 'partially checked multi-selection blocks transfer',
 'draft field opt-out changed after check blocks transfer',
 'disappeared custom draft blocks transfer',
): require(anchor in tests,'offline regression: '+anchor)
for bad in ('Database.Games.Update(', 'CatalogEnrichmentWriter.ApplyOne(',
    'journal.Prepare(', 'journal.Commit(', 'journal.MarkUndone(',
    'journal.MediaStaging.Begin(', 'journal.MediaStaging.HandOff('):
 require(bad not in ui and bad not in plan,'no second writer or journal mutation: '+bad)
require('continueButton.IsEnabled = true;' not in ui,
    'incomplete check cannot reenable handoff')
require('continueButton.IsEnabled = true;' not in prior,
    'inherited test updated for gated handoff')
versions=re.findall(r'(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$',manifest)
require(len(versions)==1 and tuple(map(int,versions[0].split('.'))) >= (0,5,40),
        'compatible package version')
require('AssemblyVersion("'+versions[0]+'.0")' in plugin and
        'AssemblyFileVersion("'+versions[0]+'.0")' in plugin,
        'assembly/file version match')
print('PASS:',checks,'native field-check gating, stale draft defense, no duplicate writer')
