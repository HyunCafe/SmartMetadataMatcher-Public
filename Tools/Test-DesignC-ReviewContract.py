#!/usr/bin/env python3
"""Static regression for read-only Design C to Classic verified review contract."""
from pathlib import Path
import re
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text('utf8')
classic=(root/'Source/CatalogWindow.cs').read_text('utf8')
contract=(root/'Source/WorkspaceReviewContract.cs').read_text('utf8')
tests=(root/'Tests/WorkspaceBatchReviewTests.cs').read_text('utf8')
proj=(root/'SmartMetadataMatcher.csproj').read_text('utf8')
tproj=(root/'Tests/NameRulesTests.csproj').read_text('utf8')
manifest=(root/'extension.yaml').read_text('utf8')
plugin=(root/'Source/Plugin.cs').read_text('utf8')
checks=0
def need(p,m):
 global checks
 checks+=1
 if not p:raise AssertionError('FAIL native review transfer contract: '+m)
for phrase in (
 'WorkspaceReviewContract verifiedReview;',
 'verifiedReview = WorkspaceReviewContract.Capture(plan,verifiedTargets,verifiedDetails);',
 'classic.StageVerifiedNativeReview(verifiedReview);',
 'classic.StageTransferredDrafts(transferDrafts);',
 'classic.Show();',
 'WorkspaceBatchLivePreflight.EnsureCurrent(checkedGame,current,',
 'WorkspaceBatchLivePreflight.SameDraft(checkedGame.Draft,',
):need(phrase in ui,'native handoff missing '+phrase)
for phrase in (
 'internal void StageVerifiedNativeReview(WorkspaceReviewContract reviewed)',
 'reviewed.EnsureCurrent(id =>',
 'ScanInput(live)',
 'internal void StageTransferredDrafts(IEnumerable<CatalogImportDraft> incoming)',
 'private bool ConfirmImportPlan(',
 'private void RunBatch(',
 'WorkspaceImportCoordinator.ApplyOne(api,enrichmentJournal,',
):need(phrase in classic,'Classic contract missing '+phrase)
for phrase in (
 'internal sealed class WorkspaceReviewContract',
 'WorkspaceBatchLivePreflight.DetailedPlanReady(rows, verified, details)',
 'target.Draft.Matches(target.GameId, target.GameName,',
 'EnrichmentPlan.IdentityKey(x.PlannedTitle)',
 'internal void EnsureCurrent(Func<Guid, CatalogInput> readCurrent)',
 'current.LocalReleaseYear != expected.LocalReleaseYear',
 'current.MissingMask != expected.MissingMask',
):need(phrase in contract,'review contract missing '+phrase)
for phrase in (
 'WorkspaceReviewContract.Capture(gateRows,gateVerified,detailMap)',
 'reviewContract.EnsureCurrent(id=>id==gateId?unchanged:null)',
 'Classic-side stale title rejected',
 'Classic-side changed missing fields rejected',
 'forged native game name rejected',
 'forged native current title rejected',
):need(phrase in tests,'C# regression missing '+phrase)
for phrase in ('<Compile Include="Source/WorkspaceReviewContract.cs" />',):
 need(phrase in proj,'DLL project compile inventory missing')
for phrase in ('<Compile Include="../Source/WorkspaceReviewContract.cs" Link="WorkspaceReviewContract.cs" />',):
 need(phrase in tproj,'test project compile inventory missing')
for bad in ('CatalogEnrichmentWriter.ApplyOne(', 'Database.Games.Update(',
            'journal.Prepare(', 'journal.Commit(', 'MediaStaging.Begin('):
 need(bad not in contract and bad not in ui,'new writer path forbidden '+bad)
need('StageVerifiedNativeReview(' in classic and classic.count('StageVerifiedNativeReview(')==1,
     'single receiver required')
versions=re.findall(r'(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$',manifest)
need(len(versions)==1 and tuple(map(int,versions[0].split('.'))) >= (0,5,42),'version wrong')
need('AssemblyVersion("'+versions[0]+'.0")' in plugin and
     'AssemblyFileVersion("'+versions[0]+'.0")' in plugin,'manifest and assembly not aligned')
print('PASS:',checks,'read-only native/Classic review contract, writer boundary, and regression checks')
