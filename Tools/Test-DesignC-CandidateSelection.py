#!/usr/bin/env python3
"""Static candidate-row selection safeguards; Windows/Playnite rendering unverified."""
from pathlib import Path
p=Path(__file__).resolve().parent.parent
ui=(p/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
start=ui.index('            var results = new DataGrid {AutoGenerateColumns = false, IsReadOnly = true,')
end=ui.index('            AddColumn(results, "IGDB game", "Title", 300);',start)
part=ui[start:end]
checks={
    'single selection': 'SelectionMode = DataGridSelectionMode.Single' in part,
    'whole-row unit': 'SelectionUnit = DataGridSelectionUnit.FullRow' in part,
    'read-only candidate table': 'IsReadOnly = true' in part,
    'explicit dark row style': 'results.RowStyle = choiceRowStyle;' in part,
    'selected-row fill': 'choiceSelected.Setters.Add(new Setter(Control.BackgroundProperty, Selection));' in part,
    'cells have no selection border': 'choiceSelectedCell.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));' in part,
    'cells have transparent border': 'choiceCellStyle.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Transparent));' in part,
    'cell border width zero': 'choiceCellStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));' in part,
    'no individual cell focus rectangle': 'choiceCellStyle.Setters.Add(new Setter(FrameworkElement.FocusVisualStyleProperty, null));' in part,
    'selected cell uses same row color': 'choiceSelectedCell.Setters.Add(new Setter(Control.BackgroundProperty, Selection));' in part,
    'consistent vertical padding': 'choiceCellStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));' in part,
    'candidate colors retained': 'AlternatingRowBackground = AlternateRow, AlternationCount = 2' in part,
    'no automatic fuzzy selection': 'results.SelectedItem = null; // Never preselect an unrelated fuzzy result.' in ui,
    'explicit user choice': 'results.SelectedItem as IdentityChoiceRow' in ui and 'Use highlighted suggestion' in ui,
    'live exact ID verification retained': 'service.FetchExact(igdbId' in ui,
    'no direct import access': all(x not in ui for x in ('Database.Games.Update(', 'new IgdbReadOnly(', '.SaveBatch(', 'RunBatch(')),
}
for label,passed in checks.items():
    if not passed:print('FAIL:',label)
if not all(checks.values()):raise SystemExit(1)
print('PASS:',len(checks),'candidate row-selection source safeguards; WPF/Playnite unverified')
