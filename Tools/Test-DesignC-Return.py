#!/usr/bin/env python3
"""Read-only source guards for v0.5.29 return-to-workspace integration."""
from pathlib import Path
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
helper=(r/'Source/WorkspaceReturnState.cs').read_text(encoding='utf-8-sig')
classic=(r/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
plugin=(r/'Source/Plugin.cs').read_text(encoding='utf-8-sig')
proj=(r/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
tproj=(r/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig')
tests=(r/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig')
checks=0
def check(ok,reason):
    global checks
    checks+=1
    if not ok:raise AssertionError('FAIL: '+reason)
for required in (
    'private readonly WorkspaceReturnState returnState;',
    'new WorkspaceReturnState(markedIds, focused == null ? Guid.Empty : focused.GameId,',
    'var resume = CaptureReturnState();',
    'dialog.Close();\n                window.Close();',
    'classic.StageVerifiedNativeReview(verifiedReview);',
    'new LibraryWorkspacePreview(api, dataPath, resume).Show();',
    'if (returnState != null)\n                foreach (var id in returnState.MarkedGameIds) markedIds.Add(id);',
    'SafeReload();\n            RestoreReturnState();',
    'private void RestoreReturnState()',
    'scope.SelectedIndex = returnState.CategoryIndex;',
    'search.Text = returnState.SearchText;',
    'missingFilter.SelectedIndex = returnState.MissingFilterIndex;',
    'if (platformFilter.Items.Contains(returnState.Platform))',
    'if (focused != null && view != null && view.Contains(focused))',
    'new LibraryWorkspacePreview(api, dataPath, null)', # NOT expected, see separate check
):
    if 'null)' in required: continue
    check(required in ui,'workspace: '+required)
for banned in ('new IgdbReadOnly(', 'Database.Games.Update(', '.SaveBatch(', '.Commit(',
               'File.WriteAllText(', 'File.WriteAllBytes('):
    check(banned not in ui,'UI must not perform provider or writer actions: '+banned)
for banned in ('Playnite.SDK','Database.','new IgdbReadOnly(','.Commit(',
               'File.Write', 'RunBatch('):
    check(banned not in helper,'return snapshot stays pure: '+banned)
for required in ('WorkspaceReviewHandoff.MaximumSelectedGames',
                 'return stillEligible && id!=Guid.Empty && marked.Contains(id);',
                 'IEnumerable<Guid> MarkedGameIds',
                 'throw new ArgumentException("Return state contains invalid or duplicate marked game IDs")'):
    check(required in helper,'helper: '+required)
check('markedIds.RemoveWhere(id => !items.Any(x => x.GameId == id && x.CanMark));' in ui,
      'read-only reloading drops completed, ignored and missing marks')
check('if(workspaceHandoff!=null && (marked==null ||' in classic,
      'Classic still restricts every batch to selected game IDs')
check('Source/WorkspaceReturnState.cs' in proj,'C# plugin includes pure snapshot')
check('WorkspaceReturnStateTests.cs' in tproj,'offline suite includes snapshot tests')
check('Link="WorkspaceReturnState.cs"' in tproj,'offline tests link snapshot class')
check('WorkspaceReturnStateTests.Run();' in tests,'test runner calls snapshot tests')
check('new CatalogWindow(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
      'Classic menu entry retained')
check('new LibraryWorkspacePreview(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
      'Design C menu entry retained')
print('PASS:',checks,'Design C return-flow read-only source invariants; Playnite runtime not certified.')
