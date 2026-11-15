#!/usr/bin/env python3
"""No Playnite dependency: assert Design C mark limits remain visible and fail closed."""
from pathlib import Path
root = Path(__file__).resolve().parent.parent
ui = (root / 'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
helper = (root / 'Source/WorkspaceReviewHandoff.cs').read_text(encoding='utf-8-sig')
cs = (root / 'Tests/WorkspaceReviewHandoffTests.cs').read_text(encoding='utf-8-sig')
assert 'internal const int MaximumSelectedGames = 1000' in helper
assert 'additional <= MaximumSelectedGames - existing' in helper
assert 'CanAddMarks(markedIds.Count, 1)' in ui
mark = ui.split('private void MarkHighlighted(bool mark)', 1)[1].split('private void MarkShown(bool mark)', 1)[0]
shown = ui.split('private void MarkShown(bool mark)', 1)[1].split('private void ShowIgnoredGames()', 1)[0]
assert 'if (mark && !WorkspaceReviewHandoff.CanAddMarks(markedIds.Count, additions))' in mark
assert mark.index('CanAddMarks') < mark.index('bulkMarking = true;')
assert 'CanAddMarks(markedIds.Count, shown.Count)' in shown
assert shown.index('CanAddMarks') < shown.index('bulkMarking = true;')
assert 'no marks changed.' in shown
assert 'Unmark some first.' in ui
assert 'catch (ArgumentException ex)' in ui.split('private void ShowMarkedPreview(Guid? focusedGameId)', 1)[1].split('var chosen =',1)[0]
assert 'Review selection unavailable' in ui
for expected in ('1001st mark rejected', 'bulk overflow rejected atomically',
                 'negative existing count rejected', 'negative addition rejected'):
    assert expected in cs
print('PASS: bounded mark selection, no partial bulk selection, visible review error; Playnite runtime unverified')
