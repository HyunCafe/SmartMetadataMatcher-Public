#!/usr/bin/env python3
"""Source safety/interaction contract for contextual Design C review actions.

Ensures the one-game and multi-game review UI retain separate scopes while
sharing the original preflight, approval, and importing functions. This is NOT
a Playnite runtime or import/undo certification.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1]
ui = (root / 'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
review = ui.split('private void ShowMarkedPreview(Guid? focusedGameId)', 1)[1].split(
    '// Native multi-game import:', 1)[0]
preflight = ui.split('private void CheckMarkedLiveFields(', 1)[1].split(
    'private static string ChangedFields(', 1)[0]
checks = {
    'focused review remains explicit scope':
        'WorkspaceReviewHandoff.ForReviewScope(markedIds, focusedGameId)' in review and
        'Marked=reviewScope.Includes(x.GameId)' in review and
        'DraftState=reviewScope.Includes(x.GameId)?CurrentDraft(x):null' in review,
    'eligibility revalidated before review':
        'chosen.Count != reviewScope.Count || chosen.Any(x => !x.CanMark)' in review,
    'either one-game entry uses identical compact dimensions':
        'bool singleGameReview = chosen.Count == 1;' in review and
        'dialog.Width = singleGameReview ? 920 : 1170;' in review and
        'dialog.Height = singleGameReview ? 400 : 630;' in review and
        'dialog.MinHeight = singleGameReview ? 370 : 440;' in review and
        'grid.MaxHeight = 105;' in review,
    'single review identifies the actual game':
        '"Review: " + chosen[0].Title' in review and
        'if (singleGameReview)' in review,
    'single review has compact separate column set':
        'AddColumn(grid, "Current title", "CurrentTitle", 200);' in review and
        'AddColumn(grid, "Title after import (proposed)", "TargetTitle", 235);' in review and
        'AddColumn(grid, "Next step", "NextStep", 245);' in review,
    'batch is a concise overview with full field plan preserved':
        'AddColumn(grid, "Game", "Game", 180);' in review and
        'AddColumn(grid, "After import", "TargetTitle", 180);' in review and
        'AddColumn(grid, "Draft", "Draft", 125);' in review and
        'AddColumn(grid, "Field check", "LiveCheck", 155);' in review and
        'AddColumn(grid, "Available fields"' not in review and
        'AddColumn(grid, "Proposed title"' not in review and
        'ShowVerifiedBatchDetails(dialog,plan,verifiedTargets,verifiedDetails)' in review,
    'single contextual apply button instead of two':
        'decisionActions.Children.Add(chosen.Count == 1 ? nativeApplyButton : nativeBatchButton);' in review and
        'decisionActions.Children.Add(nativeApplyButton);' not in review and
        'decisionActions.Children.Add(nativeBatchButton);' not in review and
        'preparationActions.Children.Add(liveCheckButton);' in review,
    'native apply visually primary and classic a fallback':
        'ActionButton("Apply this game...", "\\uE8FB"' in review and
        'ActionButton("Apply selected games...", "\\uE8FB"' in review and
        'verifiedTargets,verifiedDetails), true);' in review and
        'ActionButton("Continue to Classic review", "\\uE8A7"' in review and
        'Classic is an optional fallback.' in review,
    'identity confirmation action is not enabled when unnecessary':
        'confirmBatchIdentitiesButton.IsEnabled = hasUnconfirmed;' in review and
        'confirmBatchIdentitiesButton.Visibility = hasUnconfirmed ? Visibility.Visible : Visibility.Collapsed;' in review,
    'decorative review rows use the same passive, nonselecting grid treatment':
        'ConfigureReadOnlyReviewGrid(grid);' in review and
        'grid.PreviewMouseLeftButtonDown += preventRowSelection;' in ui and
        'grid.PreviewMouseRightButtonDown += preventRowSelection;' in ui and
        'if (hit is DataGridRow) args.Handled = true;' in ui and
        'SelectionMode = DataGridSelectionMode.Extended' not in review,
    'field details appear only with a complete exact check':
        'detailsButton.Visibility = Visibility.Collapsed;' in review and
        'detailsButton.Visibility = detailsButton.IsEnabled ? Visibility.Visible : Visibility.Collapsed;' in preflight and
        'continueButton.IsEnabled = WorkspaceBatchLivePreflight.DetailedPlanReady(' in preflight,
    'no automatic write or approval from a review':
        'No data is imported until you explicitly approve the final plan' in review and
        'Check exact fields (read-only)' in review and
        'WorkspaceBatchLivePreflight.DetailedPlanReady(' in review,
    'existing native functions invoked rather than new writer':
        'StartNativeSingleImport(chosen,plan,dialog,liveStatus,continueButton,' in review and
        'StartNativeConfirmedBatch(chosen,plan,dialog,liveStatus,continueButton,' in review and
        'new CatalogWindow(api, dataPath, handoff)' in review and
        'CatalogEnrichmentWriter.ApplyOne(' not in ui and
        'Database.Games.Update(' not in ui,
    'post-field-check instructions no longer mandate Classic':
        'Fields checked. Review the exact plan before final Design C approval.' in preflight and
        'Ready to continue to Classic final review.' not in preflight,
    'both native action enable gates retain exact preflight':
        'nativeApplyButton.IsEnabled = continueButton.IsEnabled && plan.Count==1' in preflight and
        'nativeBatchButton.IsEnabled = continueButton.IsEnabled && plan.Count>1' in preflight,
}
for name, ok in checks.items():
    if not ok:
        raise AssertionError(name)
print('PASS: %d contextual review flow/source safety checks; Playnite runtime unverified' % len(checks))
