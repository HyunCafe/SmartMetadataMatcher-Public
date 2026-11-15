#!/usr/bin/env python3
"""v0.5.59 source-only UX guards; not a substitute for WPF/Playnite runtime tests."""
from pathlib import Path
root = Path(__file__).resolve().parents[1]
ui = (root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
assert 'private void EditFocusedDraft()' in ui
assert 'if (!LiveFieldPreviewMatches(selected)) return;' in ui
assert 'WorkspaceDraftPolicy.Save(current, compared, draft, library)' in ui
assert 'if (!LiveFieldPreviewMatches(selected))' in ui
# The inspector, not the selected game table, owns the decorative preview.
a = ui.index('var proposedTitle = Label("POSSIBLE FIELD CHANGES · READ-ONLY"')
b = ui.index('draftStatus = Label("No draft saved."', a)
preview = ui[a:b]
checks = {
    'editor comes before decorative grid': ui.index('editDraftButton = ActionButton("Choose fields / custom text"') < a,
    'editor requires live evidence': 'editDraftButton.IsEnabled = !scanning && !checkingLiveFields && hasLiveFields;' in ui,
    'disabled editor shows prerequisite': '"Check IGDB fields first"' in ui and 'ToolTipService.SetShowOnDisabled(editDraftButton, true);' in ui,
    'read-only title announces intent': 'POSSIBLE FIELD CHANGES · READ-ONLY' in preview,
    'comparison does not accept text edits': 'IsReadOnly = true' in preview,
    'only comparison row clicks suppressed': 'if (hit is DataGridRow) args.Handled = true;' in preview,
    'click interception stops at grid': 'hit != fieldComparison' in preview,
    'both pointer buttons covered': 'fieldComparison.PreviewMouseLeftButtonDown += preventPreviewSelection;' in preview and 'fieldComparison.PreviewMouseRightButtonDown += preventPreviewSelection;' in preview,
    'scrollbars not suppressed globally': 'fieldComparison.IsHitTestVisible = false' not in preview and 'fieldComparison.PreviewMouseWheel' not in preview,
    'preview cells nonfocusable': 'previewCells.Setters.Add(new Setter(UIElement.FocusableProperty, false));' in preview,
    'preview rows nonfocusable': 'previewRows.Setters.Add(new Setter(UIElement.FocusableProperty, false));' in preview,
    'main game selection preserved': 'table.SelectionChanged += (sender, args) => {UpdateInspector(); UpdateSelectionSummary();};' in ui,
    'main right-click menu remains': 'table.PreviewMouseRightButtonDown += (sender,args) =>' in ui and 'table.ContextMenu = context;' in ui,
    'no accidental addition of Playnite write': all(x not in ui for x in ('api.Database.Games.Update(', 'CatalogEnrichmentWriter.ApplyOne(', 'CatalogEnrichmentWriter.UndoLast(', 'new IgdbReadOnly(')),
    'still uses existing session-only draft': 'draftStates[selected.GameId] = saved;' in ui,
    'workable importer clearly named': 'final Design C approval' in ui and
        'StartNativeSingleImport(chosen,plan,dialog' in ui and
        'StartNativeConfirmedBatch(chosen,plan,dialog' in ui,
    'no stale Classic-only import claim': 'imports still use Classic' not in ui,
    'launch menu does not claim read-only': 'Open Library Workspace (Design C preview)...' in (root/'Source/Plugin.cs').read_text(encoding='utf-8-sig'),
}
fail = [name for name, value in checks.items() if not value]
if fail: raise SystemExit('FAIL: '+', '.join(fail))
print('PASS:', len(checks), 'field-preview and draft-entry UX source guards; verify pointer behavior in Windows/Playnite')
