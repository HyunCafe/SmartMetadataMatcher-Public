#!/usr/bin/env python3
"""v0.5.67 coordinated Design C presentation checks, not WPF runtime tests."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
show=s.split('internal void Show()',1)[1].split('private void RestoreReturnState()',1)[0]
review=s.split('private void ShowMarkedPreview(Guid? focusedGameId)',1)[1].split('// Native multi-game import:',1)[0]
history=s.split('private void ShowHistoryPreview()',1)[1].split('private static string SavedEvidence(',1)[0]
checks={
 'main review is the first action in selection tray':
    show.index('trayActions.Children.Add(reviewMarksButton);')<show.index('trayActions.Children.Add(markHighlightedButton);'),
 'marking and review still distinct':
    'ShowMarkedPreview, true' in show and '() => MarkHighlighted(true)' in show,
 'one-game primary review remains scoped':
    'ReviewFocusedGame, true' in show and 'WorkspaceReviewHandoff.ForReviewScope(markedIds, focusedGameId)' in review,
 'game action card is above comparison':
    show.index('detail.Children.Add(nextCard);') < show.index('var proposedTitle = Label('),
 'existing live-check and draft handlers retained':
    'CheckLiveFieldAvailability);' in show and 'EditFocusedDraft);' in show,
 'secondary match controls grouped':
    'Header = "Manage IGDB match / ignore game"' in show and 'manageBody.Children.Add(identityActions);' in show,
 'identity control methods are unchanged':
    all(x in show for x in ('StartSavedRefresh(false)', 'ShowIdentitySearch);','StartConfirmIdentity);','ForgetFocusedIdentity);','ToggleFocusedIgnore);')),
 'review dialog separates preparation from approval':
    all(x in review for x in ('preparationActions.Children.Add(confirmBatchIdentitiesButton);','preparationActions.Children.Add(liveCheckButton);','preparationActions.Children.Add(detailsButton);','decisionActions.Children.Add(chosen.Count == 1 ? nativeApplyButton : nativeBatchButton);')),
 'Classic fallback and final approval kept':
    'decisionActions.Children.Add(continueButton);' in review and 'No data is imported until you explicitly approve the final plan' in review,
 'one-item review does not fill an oversized empty grid':
    'grid.MaxHeight = 105;' in review and 'dialog.Height = singleGameReview ? 400 : 630;' in review,
 'History conditional undo still accessible':
    'UndoLatestFromHistory(dialog)' in history and 'UndoSelectedFromHistory(dialog,grid)' in history,
 'History visibility options are view-only':
    'ChangeHistoryVisibility(grid,true,refreshHistoryView)' in history and 'ChangeHistoryVisibility(grid,false,refreshHistoryView)' in history and 'journal.AllEntries()' in history,
 'no direct Playnite writes introduced':
    all(x not in s for x in ('Database.Games.Update(','.SaveBatch(','.MarkUndone(','.Commit(')),
}
fail=[k for k,v in checks.items() if not v]
for x in fail:print('FAIL:',x)
if fail:raise SystemExit(1)
print('PASS:',len(checks),'cohesive layout and unchanged navigation/write-boundary source checks; runtime unverified')
