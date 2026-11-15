#!/usr/bin/env python3
"""Read-only Design C handoff safety invariants, not a host runtime certification."""
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
ui=(ROOT/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
classic=(ROOT/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
helper=(ROOT/'Source/WorkspaceReviewHandoff.cs').read_text(encoding='utf-8-sig')
csproj=(ROOT/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
tests=(ROOT/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig')
testproj=(ROOT/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig')
checks=0
def check(b,description):
    global checks
    checks+=1
    if not b: raise AssertionError('FAIL '+description)
for expected in ('new WorkspaceReviewHandoff(chosen.Select(x => x.GameId))',
                 'classic.StageVerifiedNativeReview(verifiedReview);',
                 'Continue to Classic review','not yet approved',
                 'WorkspaceNativeConfirmedBatch.Execute(',
                 'new CatalogWindow(api, dataPath).Show()'):
    check(expected.casefold() in ui.casefold(), 'UI: '+expected)
for forbidden in ('Database.Games.Update(', 'new IgdbReadOnly(', '.SaveBatch(', '.Commit('):
    check(forbidden not in ui, 'read-only workspace: '+forbidden)
for expected in ('private readonly WorkspaceReviewHandoff workspaceHandoff;',
                 'private readonly HashSet<Guid> handoffMarksPending',
                 'WorkspaceReviewHandoff selection) : this(apiValue, pluginDataPath)',
                 'queue=queue.Where(x=>workspaceHandoff.Includes(x.Id)).ToList();',
                 'ApplyWorkspaceHandoffMarks();',
                 'reviewSources=workspaceHandoff==null ? results.ToList()',
                 'results.Where(x=>workspaceHandoff.Includes(x.GameId)).ToList();',
                 'var d=decisions[i];var r=reviewSources[i];',
                 'marked.Any(x=>x==null || !workspaceHandoff.Includes(x.GameId))',
                 'verifiedBatchButton.IsEnabled=workspaceHandoff==null',
                 'if(workspaceHandoff!=null)',
                 'EnrichmentJobPlanner.Create(candidates,library,true,false)',
                 'WorkspaceReviewHandoff.CanMark',
                 'private void RefreshSelectedCatalogMatches('):
    check(expected in classic, 'Classic: '+expected)
check(classic.index('marked.Any(x=>x==null || !workspaceHandoff.Includes(x.GameId))') <
      classic.index('if(!JournalAllowsNewWrites()) return;',classic.index('private void RunBatch(')),
      'scope gate runs before write authorization')
check(classic.count('reviewSources=workspaceHandoff==null')==1,'one scoped review source')
check(classic.count('var candidates=results.Select(x=>new EnrichmentCandidate {')==1,
      'automatic plan retained, and only review plan altered')
for expected in ('internal const int MaximumSelectedGames = 1000',
                 'ids.Distinct()', 'ids.Contains('):
    if expected=='ids.Distinct()': expected='values.Distinct()'
    check(expected in helper,'selection helper: '+expected)
check('WorkspaceReviewHandoffTests.Run();' in tests,'C# checks hooked')
check('<Compile Include="Source/WorkspaceReviewHandoff.cs" />' in csproj,'DLL compile inventory')
check('<Compile Include="../Source/WorkspaceReviewHandoff.cs" Link="WorkspaceReviewHandoff.cs" />' in testproj,
      'offline test compile inventory')
check('<Compile Include="WorkspaceReviewHandoffTests.cs" />' in testproj,'offline tests included')
for forbidden in ('File.Write', 'Playnite.SDK', 'CatalogEnrichmentWriter', 'EnrichmentJournal'):
    check(forbidden not in helper,'pure helper: '+forbidden)
check('useCatalogTitle' not in classic, 'cleanup option state removed')
print('PASS:',checks,'Design C selected-only handoff source assertions; no Playnite access')
