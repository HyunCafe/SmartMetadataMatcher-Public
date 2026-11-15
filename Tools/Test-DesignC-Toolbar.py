#!/usr/bin/env python3
"""Pure read-only Design C toolbar invariants. Not a WPF/Playnite UI certification."""
from pathlib import Path

root = Path(__file__).resolve().parent.parent
src = (root / 'Source' / 'LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
checks = {
    'four-column responsive grid': 'var filterRow = new Grid' in src and
        'filterRow.ColumnDefinitions.Add(new ColumnDefinition' in src,
    'filter controls not in WrapPanel': 'var filterRow = new WrapPanel' not in src,
    'search grows to available width': 'Width = new GridLength(1, GridUnitType.Star), MinWidth = 200' in src,
    'shared height constant': 'private const double ToolbarHeight = 36;' in src,
    'shared horizontal gap': 'private const double ToolbarGap = 8;' in src,
    'group spacing': 'private const double ToolbarGroupGap = 16;' in src,
    'all action buttons share baseline': 'Height = ToolbarHeight, Margin = new Thickness(0, 0, ToolbarGap, 0)' in src,
    'search baseline': 'search = new TextBox {Height = ToolbarHeight' in src,
    'platform baseline': 'platformFilter = new ComboBox {Width = 180, Height = ToolbarHeight' in src,
    'field baseline': 'missingFilter = new ComboBox {Width = 180, Height = ToolbarHeight' in src,
    'no bottom offset in filters': 'Margin = new Thickness(0, 0, 0, 4)' not in src[src.index('var filterRow = new Grid'):src.index('var columns = new Grid')],
    'utility group distinct': 'var separator = new Border {Width = 1, Height = 24' in src,
    'reset is compact': 'CompactToolbarButton("Reset", "\\uE777"' in src,
    'reload is compact': 'CompactToolbarButton("Reload", "\\uE72C"' in src,
    'reload still uses SafeReload': '"\\uE72C", SafeReload,' in src,
    'clear is in search host': 'searchHost.Children.Add(searchClearButton);' in src,
    'clear does not take fixed grid column': 'Grid.SetColumn(searchClearButton' not in src,
    'clear hidden for empty search': 'searchClearButton.Visibility = hasSearch ? Visibility.Visible : Visibility.Collapsed' in src,
    'search hint hidden after typing': 'searchPlaceholder.Visibility = hasSearch ? Visibility.Collapsed : Visibility.Visible' in src,
    'reset disabled when filters inactive': 'toolbarResetButton.IsEnabled = hasSearch ||' in src,
    'reset resets search and filters': 'search.Clear(); platformFilter.SelectedIndex = 0; missingFilter.SelectedIndex = 0;' in src,
    'state updates after edits': all(x in src for x in (
        'search.TextChanged += (sender, args) => {ApplyFilter(); UpdateToolbarActions();}',
        'platformFilter.SelectionChanged += (sender, args) => {ApplyFilter(); UpdateToolbarActions();}',
        'missingFilter.SelectionChanged += (sender, args) => {ApplyFilter(); UpdateToolbarActions();}')),
    'accessible search': 'AutomationProperties.SetName(search, "Search games")' in src,
    'accessible clear': 'AutomationProperties.SetName(searchClearButton, "Clear search")' in src,
    'accessible filters': all(x in src for x in (
        'AutomationProperties.SetName(platformFilter, "Filter by platform")',
        'AutomationProperties.SetName(missingFilter, "Filter by missing metadata")')),
    'keyboard focus and reload retained': 'Key.F5' in src and 'Key.F' in src and 'Key.Escape' in src,
    'no Playnite write calls': all(x not in src for x in (
        'Database.Games.Update(', 'Database.Games.Remove(', 'Database.AddFile(', 'Database.RemoveFile(')),
    'no journal/cache write calls': all(x not in src for x in (
        '.Commit(', '.SaveBatch(', '.MarkUndone(', 'File.WriteAllText(', 'File.WriteAllLines(')),
    'no new IGDB requests': 'new IgdbReadOnly(' not in src,
    'preview selection still separate': 'private readonly HashSet<Guid> markedIds' in src and
        'SelectionMode = DataGridSelectionMode.Extended' in src,
}
for name, ok in checks.items():
    if not ok:
        print('FAIL:', name)
if not all(checks.values()):
    raise SystemExit(1)
print('PASS:', len(checks), 'toolbar/layout source invariants; runtime appearance must be checked in Playnite.')
