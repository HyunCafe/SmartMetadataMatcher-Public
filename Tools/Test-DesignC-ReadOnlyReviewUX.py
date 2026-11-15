#!/usr/bin/env python3
"""Review-modal interaction regression guards; SOURCE ONLY, not WPF behavior evidence.

The main library and IGDB identity chooser must retain their real selections.
Only two informational review grids may use passive row presentation.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
helper=ui.split('private static void ConfigureReadOnlyReviewGrid(DataGrid grid)',1)[1].split('private static Button ActionButton(',1)[0]
review=ui.split('private void ShowMarkedPreview(Guid? focusedGameId)',1)[1].split('// Native multi-game import:',1)[0]
details=ui.split('private void ShowVerifiedBatchDetails(Window owner,',1)[1].split('// Inspect marked exact IGDB records',1)[0]
assert review.count('ConfigureReadOnlyReviewGrid(grid);') == 1
assert details.count('ConfigureReadOnlyReviewGrid(grid);') == 1
checks={
 'both mouse buttons suppress clicks on informational rows only':
    all(t in helper for t in (
        'grid.PreviewMouseLeftButtonDown += preventRowSelection;',
        'grid.PreviewMouseRightButtonDown += preventRowSelection;',
        'hit != grid && !(hit is DataGridRow)',
        'if (hit is DataGridRow) args.Handled = true;')),
 'controls explicitly remain out of tab and WPF currency selections':
    all(t in helper for t in ('grid.IsTabStop = false;', 'grid.Focusable = false;',
        'grid.IsSynchronizedWithCurrentItem = false;',
        'UIElement.FocusableProperty, false')),
 'alternate shades and hover/selected fills do not imply a decision':
    all(t in helper for t in ('grid.AlternatingRowBackground = Panel;',
        'UIElement.IsMouseOverProperty', 'DataGridCell.IsSelectedProperty',
        'DataGridRow.IsSelectedProperty')),
 'scrollbars and tooltip hit testing remain available':
    'grid.IsHitTestVisible = false' not in helper and
    'grid.PreviewMouseWheel' not in helper and
    'HorizontalGridLinesBrush = SoftStroke' in helper,
 'read-only plan explicitly describes controls and approval':
    'Rows below are read-only summaries, not selections;' in review and
    'No data is imported until you explicitly approve the final plan' in review and
    'Saved IGDB suggestions are not write evidence.' in review,
 'comparison points back to final approval without a second writer':
    'Comparison only: rows cannot be selected or edited.' in details and
    'Back to review' in details and
    'choose Apply for the final itemized approval' in details and
    'Final approval independently rechecks the identity' in details,
 'one-pass visual change only; native gates and fallback retained':
    'StartNativeSingleImport(chosen,plan,dialog' in review and
    'StartNativeConfirmedBatch(chosen,plan,dialog' in review and
    'new CatalogWindow(api, dataPath, handoff)' in review and
    'Continue to Classic review' in review,
 'main library/identity chooser keep interactive selection':
    'table.SelectionChanged += (sender, args) => {UpdateInspector(); UpdateSelectionSummary();};' in ui and
    'results.SelectionChanged += (sender, args) =>' in ui and
    ui.count('ConfigureReadOnlyReviewGrid(grid);') == 2,
 'no added direct Playnite writes':
    all(t not in helper + review + details for t in ('Database.Games.Update(',
        'CatalogEnrichmentWriter.ApplyOne(', 'File.WriteAllText(')),
}
failed=[name for name,passed in checks.items() if not passed]
if failed:raise AssertionError('FAIL: '+', '.join(failed))
print('PASS:',len(checks),'review-grid static interaction guards; verify actual WPF behavior on Windows')
