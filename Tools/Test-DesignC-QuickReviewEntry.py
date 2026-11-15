#!/usr/bin/env python3
"""Targeted source contract: quick review navigation without changing approval.

No WPF runtime, Playnite API, credentials, or metadata writes are exercised.
"""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
ui = (root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
click = ui.split('table.MouseDoubleClick += (sender, args) => {',1)[1].split(
    'table.PreviewMouseRightButtonDown +=',1)[0]
keys = ui.split('table.PreviewKeyDown += (sender, args) => {',1)[1].split(
    'table.MouseDoubleClick +=',1)[0]
menu = ui.split('var context = new ContextMenu();',1)[1].split('table.ContextMenu = context;',1)[0]
details = ui.split('private void ShowFocusedGameDetails()',1)[1].split(
    'private void ReviewFocusedGame()',1)[0]
focused = ui.split('private void ReviewFocusedGame()',1)[1].split(
    'private void ShowMarkedPreview()',1)[0]
checks = {
    'double click targets real row rather than selected stale row':
        'var hitRow = visual as DataGridRow;' in click and
        'if (hitRow == null' in click and
        'if (table.SelectedItem != current) table.SelectedItem = current;' in click,
    'double click mark is excluded':
        'if (visual is CheckBox) return;' in click and
        'clickedCell.Column == checkedColumn' in click,
    'no dialog on blank or header clicks':
        'if (hitRow == null' in click and 'if (current == null) return;' in click,
    'eligible double click reuses existing focused review':
        'reviewFocusedButton.IsEnabled' in click and 'ReviewFocusedGame();' in click and
        'ShowMarkedPreview(' not in click,
    'ineligible double click preserves old details':
        'else ShowFocusedGameDetails();' in click,
    'Ctrl Enter only uses enabled focused review':
        'args.Key == Key.Enter' in keys and
        'Keyboard.Modifiers == ModifierKeys.Control' in keys and
        'reviewFocusedButton.IsEnabled' in keys and 'ReviewFocusedGame();' in keys,
    'Ctrl M mark shortcut remains separate':
        'args.Key == Key.M' in keys and 'MarkHighlighted(true)' in keys,
    'old details accessible from context menu':
        'new MenuItem {Header="View game details (read-only)..."}' in menu and
        'viewDetails.Click += (sender,args) => ShowFocusedGameDetails();' in menu and
        'viewDetails.IsEnabled=item!=null;' in menu,
    'details remain read-only and do not look up IGDB':
        'api.Dialogs.ShowMessage(' in details and
        '"Game details — read-only"' in details and
        'FetchExact' not in details and 'Database.Games.Update' not in details,
    'focused review continues to enforce eligibility and busy gate':
        'BusyForNavigation()' in focused and
        'row.FromSaved' in focused and 'row.CanMark' in focused and
        'Int64.TryParse(row.CachedCatalogId' in focused and
        'ShowMarkedPreview(row.GameId)' in focused,
    'context focused review unaffected':
        'reviewOne.Click += (sender,args) => ReviewFocusedGame();' in menu,
    'shortcuts documented in UI':
        'Ctrl+Enter: review focused game or find missing IGDB match' in ui and
        'Double-click game row: review, find missing IGDB match, or view details' in ui,
    'no new writer or silent mark in navigation':
        all(x not in click+keys+details for x in
            ('Database.Games.Update(', 'markedIds.Add(', '.Marked = true',
             'CatalogEnrichmentWriter', 'StartNativeSingleImport(', 'StartNativeConfirmedBatch(')),
}
for name, ok in checks.items():
    if not ok: raise AssertionError(name)
print('PASS: %d quick-review navigation and preservation source guards; WPF runtime unverified' % len(checks))
