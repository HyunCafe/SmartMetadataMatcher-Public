#!/usr/bin/env python3
"""Native verified field-by-field proposal, inherited safety and version checks."""
from pathlib import Path
import re
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text('utf-8-sig')
plan=(r/'Source/WorkspaceBatchReview.cs').read_text('utf-8-sig')
tests=(r/'Tests/WorkspaceBatchReviewTests.cs').read_text('utf-8-sig')
prior=(r/'Tools/Test-DesignC-PreflightGate.py').read_text('utf-8-sig')
manifest=(r/'extension.yaml').read_text('utf-8-sig')
plugin=(r/'Source/Plugin.cs').read_text('utf-8-sig')
checks=0
def need(ok,msg):
 global checks
 checks+=1
 if not ok:raise AssertionError('v0.5.41 field review: '+msg)
for x in (
 'var verifiedDetails = new Dictionary<Guid,List<WorkspaceVerifiedFieldRow>>();',
 'WorkspaceBatchLivePreflight.DetailedPlanReady(plan, verifiedTargets,',
 'ShowVerifiedBatchDetails(dialog,plan,verifiedTargets,verifiedDetails)',
 'private void ShowVerifiedBatchDetails(Window owner,',
 'CurrentIdentityInput(items.Single(x=>x.GameId==row.GameId))',
 'ConfirmUnchangedLookup(game,target.IgdbId.ToString(',
 'WorkspaceBatchLivePreflight.EnsureCurrent(target,game,',
 'WorkspaceBatchLivePreflight.SameDraft(target.Draft,',
 'var exactRows=new List<WorkspaceVerifiedFieldRow>();',
 'WorkspaceDraftPolicy.ActionLabel(field,',
 'verifiedDetails[entry.Item1.GameId]=exactRows;',
 'verifiedDetails.Remove(entry.Item1.GameId);',
 'verifiedDetails.Clear();',
 'detailsButton.IsEnabled = continueButton.IsEnabled;',
 'continueButton.IsEnabled = WorkspaceBatchLivePreflight.DetailedPlanReady(',
 'AddColumn(grid, "After import", "TargetTitle", 180);',
 'AddColumn(grid,"Current Playnite value","Current",245);',
 'AddColumn(grid,"Proposed handling","PlannedAction",250);',
 'Final approval independently rechecks the identity, current Playnite fields,',
 'Classic is available as a fallback.',
):need(x in ui,'native UI: '+x)
for x in (
 'internal sealed class WorkspaceVerifiedFieldRow',
 'public string TargetTitle { get; set; }',
 'public string CurrentTitle { get; set; }',
 'internal static bool DetailedPlanReady(',
 'if (!ReadyForHandoff(rows,verified) || details == null) return false;',
 'fields.Select(x=>x.Field).Distinct(StringComparer.Ordinal).Count()!=fields.Count',
 'titleRows.Count!=1',
 'String.Equals(titleRows[0].PlannedAction,row.TargetTitle,StringComparison.Ordinal)',
 'TargetTitle=titleValid ? title : ""',
 'StrictEvidence = x.StrictEvidence',
):need(x in plan,'pure verified row: '+x)
for x in (
 'actual Playnite current and target titles are shown separately',
 'field-by-field plan required before transfer',
 'altered title plan rejects transfer',
 'field plan from a different IGDB record rejects transfer',
 'duplicate field row rejects transfer',
 'unverified status never promotes a stale field table',
):need(x in tests,'C# negative test: '+x)
for bad in ('Database.Games.Update(', 'CatalogEnrichmentWriter.ApplyOne(',
 'journal.Prepare(', 'journal.Commit(', 'journal.MarkUndone('):
 need(bad not in ui and bad not in plan,'second writer/journal mutation forbidden: '+bad)
need('Version: 0.5.40' not in prior,'no inherited hardcoded version')
m=re.findall(r'(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$',manifest)
need(len(m)==1 and tuple(map(int,m[0].split('.'))) >= (0,5,41),'version >=0.5.41')
need('AssemblyVersion("'+m[0]+'.0")' in plugin and
     'AssemblyFileVersion("'+m[0]+'.0")' in plugin,'version consistency')
print('PASS:',checks,'native itemized IGDB/Playnite proposal and stronger handoff guard')
