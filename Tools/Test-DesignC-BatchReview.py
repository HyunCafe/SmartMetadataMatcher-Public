#!/usr/bin/env python3
"""Source invariants for native marked-batch preview and default title cleanup."""
from pathlib import Path
R=Path(__file__).resolve().parent.parent
ui=(R/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
classic=(R/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
plan=(R/'Source/WorkspaceBatchReview.cs').read_text(encoding='utf-8-sig')
tests=(R/'Tests/WorkspaceBatchReviewTests.cs').read_text(encoding='utf-8-sig')
project=(R/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
testproj=(R/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig')
runner=(R/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig')
checks=0
def check(pred,msg):
    global checks
    checks+=1
    if not pred:raise AssertionError('FAIL: '+msg)
for text in ('WorkspaceBatchReview.Preview(all, library)',
    'new WorkspaceBatchReviewInput {','new DataGrid','"After import"',
    '"Identity"','"Draft"','"Next step"',
    'WorkspaceNativeConfirmedBatch.Execute(',
    'No data is imported until you explicitly approve the final plan',
    'new WorkspaceReviewHandoff(chosen.Select(x => x.GameId))',
    'classic.StageTransferredDrafts(transferDrafts);',
    'var classic = new CatalogWindow(api, dataPath, handoff);'):
    check(text in ui,'UI: '+text)
for text in ('EnrichmentJobPlanner.Create(candidates, names, true, true)',
    'StrictEvidence = x.StrictEvidence','ResolveTitle(current.GameName,current.CandidateTitle,',
    'current.GameId,current.Platform,plannedNames,true,draft)',
    'plannedNames[index] = Tuple.Create(current.GameId,title,current.Platform);',
    'WorkspaceReviewHandoff.MaximumSelectedGames',
    'selected.Any(x => x.Completed || x.Ignored || x.MissingCount <= 0)',
    'staleDraft = !hasId','current.DraftState.Matches(live, igdbId,current.CandidateTitle)',
    '"Eligible for live preflight · not approved"'):
    check(text in plan,'pure plan: '+text)
for bad in ('Database.Games.Update(', 'new IgdbReadOnly(','.SaveBatch(',
            'new EnrichmentJournal(', 'RunBatch(', 'File.Write', 'new MetadataJournal('):
    check(bad not in plan,'no write path in pure plan: '+bad)
check('useCatalogTitle' not in classic,'obsolete title-cleanup checkbox and state removed')
check('Content="Clean titles on import"' not in classic,'no title-cleanup option anywhere in Classic controls')
for text in ('CatalogImportDraft.ResolveTitle(row.CurrentName,row.Candidate,row.GameId,',
             'row.Platform,library,true,draft)',
             'EnrichmentJobPlanner.Create(candidates,library,true,false)',
             'const bool rename=true;',
             'PlanBatchTitles(items,rename,automatic)',
             'Smart-inferred identities are still protected',
             '!(automatic && item.Row.SmartAutoEvidence && !item.Row.UserConfirmed)',
             'libraryNames,true,selectedDraft)',
             'var nameRequested=' ):
    check((text not in classic) if text=='var nameRequested=' else (text in classic),
          'standard safe title cleaning: '+text)
check('Title cleanup disabled' not in classic,'obsolete disabled-cleanup preview removed')
check('WorkspaceBatchReviewTests.Run();' in runner,'C# runner wired')
check('<Compile Include="Source/WorkspaceBatchReview.cs" />' in project,'DLL compile wired')
check('<Compile Include="WorkspaceBatchReviewTests.cs" />' in testproj,'C# tests wired')
check('WorkspaceBatchReview.Preview(games,library)' in tests,'pure review tests present')
check('same-batch duplicate display titles blocked' in tests,'batch-wide title collision covered')
check('Session draft (not transferred to Classic)' not in plan,'draft handoff is no longer described as lost')
print('PASS:',checks,'native batch preview/default-title source checks; no Playnite or Windows compilation')
