#!/usr/bin/env python3
"""Read-only static invariants for v0.5.28 selected-only guided preparation."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
cl=(root/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
helper=(root/'Source/WorkspaceReviewPreparation.cs').read_text(encoding='utf-8-sig')
tests=(root/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig')
testproj=(root/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig')
csproj=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
checks=0
def expect(value,description):
    global checks
    checks+=1
    if not value:raise AssertionError('FAIL: '+description)
for x in (
    'private bool handoffPreparing;',
    'private void StartWorkspaceReviewPreparation()',
    'private void StopWorkspaceReviewPreparation(',
    'private void ContinueWorkspaceReviewPreparation()',
    'WorkspaceReviewPreparation.Next(pending,saved.Count,false)',
    'WorkspacePreparationStep.ScanPending',
    'WorkspacePreparationStep.RefreshSaved',
    'AddButton(actions,"Prepare selected review",StartWorkspaceReviewPreparation,out handoffPrepareButton);',
    'handoffPrepareButton.Visibility=workspaceHandoff==null?Visibility.Collapsed:Visibility.Visible;',
    'int previous=scanSize.SelectedIndex;',
    'scanSize.SelectedIndex=2;',
    'StartScan();',
    'RefreshSelectedCatalogMatches(saved);',
    'workspaceHandoff.Includes(x.GameId) &&',
    'queue=queue.Where(x=>workspaceHandoff.Includes(x.Id)).ToList();',
    'marked.Any(x=>x==null || !workspaceHandoff.Includes(x.GameId))',
    'StopWorkspaceReviewPreparation("Lookup stopped or was incomplete")',
    'StopWorkspaceReviewPreparation("Refresh stopped or did not complete")',
    'fresh.Count!=targets.Count',
    'token.IsCancellationRequested',
    'handoffIdentityChanges+=changed;',
    'Confirm any changed matches manually', # optional fail if exact case
    'ShowPlanReview();',
    'MessageBoxButton.YesNo,MessageBoxImage.Question',
    'if(workspaceHandoff!=null && handoffPreparing)',
):
    if x=='Confirm any changed matches manually':
        x='confirm any changed matches manually'
    expect(x in cl,'Classic: '+x)
expect(cl.index('marked.Any(x=>x==null || !workspaceHandoff.Includes(x.GameId))') <
    cl.index('if(!JournalAllowsNewWrites()) return;',cl.index('private void RunBatch(')),
    'pre-existing final batch scope guard precedes journal checks')
expect(cl.count('window.Loaded+=(s,e)=>window.Dispatcher.BeginInvoke(')==1,
    'single auto-start in selected-only startup')
expect('classic.StageVerifiedNativeReview(verifiedReview);' in ui,'selected-only handoff preserved')
expect('Check exact fields (read-only)' in ui and
       'Saved IGDB suggestions are not write evidence' in ui,
       'read-only network check and saved-evidence limits disclosed')
expect('No data is imported until you explicitly approve the final plan' in ui and
       'acknowledge a restorable backup' in ui and
       'Review changes' in ui and
       'ShowVerifiedBatchDetails(dialog,plan,verifiedTargets,verifiedDetails)' in ui,
       'final approval and itemized review requirement disclosed')
for banned in ('new IgdbReadOnly(', 'Database.Games.Update(', '.SaveBatch(', '.Commit('):
    expect(banned not in ui,'UI no provider/writer side effects: '+banned)
for banned in ('Playnite.SDK','Database.','new IgdbReadOnly','File.Write','RunBatch'):
    expect(banned not in helper,'pure preparation policy: '+banned)
expect('WorkspaceReviewPreparationTests.Run();' in tests,'test runner wired')
expect('Source/WorkspaceReviewPreparation.cs' in csproj,'DLL project wired')
expect('WorkspaceReviewPreparationTests.cs' in testproj,'test runner project wired')
expect('Link="WorkspaceReviewPreparation.cs"' in testproj,'pure policy linked into test suite')
print('PASS:',checks,'guided selected-only preparation static guards; Windows/Playnite runtime unverified')
