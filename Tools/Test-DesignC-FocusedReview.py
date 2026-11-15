#!/usr/bin/env python3
"""Source-level guards for focused-only review; not Playnite runtime or writer tests."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
ui = (root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
policy = (root/'Source/WorkspaceReviewHandoff.cs').read_text(encoding='utf-8-sig')
tests = (root/'Tests/WorkspaceReviewHandoffTests.cs').read_text(encoding='utf-8-sig')

def section(begin,end):
    assert begin in ui, begin
    return ui.split(begin,1)[1].split(end,1)[0]

focused = section('private void ReviewFocusedGame()', 'private void ShowMarkedPreview()')
review = section('private void ShowMarkedPreview(Guid? focusedGameId)', '// Native multi-game import:')
checks = {
    'focused review checks for running operations': 'BusyForNavigation()' in focused,
    'focused review requires saved identity': all(x in focused for x in ('row.FromSaved','row.CanMark','row.CachedCatalogId','igdbId <= 0','row.CachedCatalogName')),
    'focused review opens its own scope': 'ShowMarkedPreview(row.GameId)' in focused,
    'marked review remains available': 'private void ShowMarkedPreview() { ShowMarkedPreview(null); }' in ui,
    'review scope delegated to safe ID policy': 'WorkspaceReviewHandoff.ForReviewScope(markedIds, focusedGameId)' in review,
    'review excludes other marks from plan': 'Marked=reviewScope.Includes(x.GameId)' in review,
    'review excludes other drafts from plan': 'DraftState=reviewScope.Includes(x.GameId)?CurrentDraft(x):null' in review,
    'review fails closed on removed or ineligible row': 'chosen.Count != reviewScope.Count || chosen.Any(x => !x.CanMark)' in review,
    'ordinary bulk action remains explicit': 'ActionButton("Review marked games", "\\uE8A7", ShowMarkedPreview, true)' in ui,
    'inspector uses same focused action': 'ActionButton("Review this game...", "\\uE8A7", ReviewFocusedGame, true)' in ui,
    'context menu uses same focused action': 'reviewOne.Click += (sender,args) => ReviewFocusedGame();' in ui,
    'focused button supports saved candidate or exact-ID recovery': 'reviewFocusedButton.IsEnabled = !BusyForNavigation() && (hasSavedId || needsMatch);' in ui and 'NeedsManualIdentity(row)' in ui,
    'review does not accidentally mark additional rows': all(x not in focused for x in ('markedIds.Add(', '.Marked = true', 'MarkHighlighted(')),
    'scope rejects empty, oversized or duplicate ID input': 'MaximumSelectedGames' in policy and 'values.Distinct().Count() != values.Count' in policy,
    'focused scope never falls back to other marks': 'new WorkspaceReviewHandoff(new[] { focusedGameId.Value })' in policy,
    'behavioral tests for exact focused scope': 'focused-only projection does not leak marked games' in tests and 'marked review keeps existing exact scope' in tests,
    'no new Playnite writes in review UI': all(x not in ui for x in ('Database.Games.Update(', 'Database.Games.Remove(', '.MarkUndone(', 'File.WriteAllText(')),
}
for name, passed in checks.items():
    if not passed:
        raise AssertionError(name)
print('PASS: %d focused-review source guards; C# scope assertions run in offline Windows suite.' % len(checks))
