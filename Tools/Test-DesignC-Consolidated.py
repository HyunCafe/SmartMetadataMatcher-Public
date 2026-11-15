#!/usr/bin/env python3
"""Read-only source invariants for 0.5.24 UX; not a runtime UI or WPF compiler test."""
from pathlib import Path

root = Path(__file__).resolve().parent.parent
ui = (root / 'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
plugin = (root / 'Source/Plugin.cs').read_text(encoding='utf-8-sig')
checks = {
    'one navigation control': 'NavigationTab("Suggestions"' in ui and 'scope.Items.Add(label)' in ui
        and 'filterRow.Children.Add(scope)' not in ui,
    'six states including other and all': all(x in ui for x in (
        'NavigationTab("Needs review"', 'NavigationTab("To check"', 'NavigationTab("Updated"',
        'NavigationTab("Other"', 'NavigationTab("All games"')),
    'category only changes view scope': 'scope.SelectedIndex = index' in ui and
        'button.Click += (sender, args) => scope.SelectedIndex = index' in ui,
    'explicit history read-only': ('private void ShowHistoryPreview()' in ui and 'journal.AllEntries()' in ui and 'DataGrid {AutoGenerateColumns = false, IsReadOnly = true' in ui and 'UndoLatestFromHistory(dialog)' in ui and 'WorkspaceHistoryUndo.PreviewLatest(dataPath)' in ui and 'WorkspaceHistoryUndo.UndoLatest(api,dataPath,latest.OperationId)' in ui and 'WorkspaceHistoryUndo.InspectRecovery(dataPath)' in ui and 'if (confirm.ShowDialog()!=true) return;' in ui and 'backup.Checked+=(s,e)=>undo.IsEnabled=true;' in ui and 'backup.Unchecked+=(s,e)=>undo.IsEnabled=false;' in ui and 'CatalogEnrichmentWriter.UndoLast(' not in ui),
    'no ambiguous check-or-update label': 'Check or update games' not in ui and
        'Open Classic Matcher' in ui,
    'single search control': 'search = new TextBox' in ui and
        'searchClearButton.Click' in ui and 'toolbarResetButton' in ui,
    'persistent across filters via GameId': 'markedIds' in ui and
        'item.Marked = item.CanMark && markedIds.Contains(item.GameId)' in ui,
    'highlight remains distinct from checked marks': 'SelectionMode = DataGridSelectionMode.Extended' in ui
        and 'Mark highlighted' in ui and 'Binding("Marked")' in ui,
    'mark tray warns about outside-filter marks': 'outside this view' in ui,
    'marked list requires explicit confirmation before handoff': 'ShowMarkedPreview()' in ui and
        'WorkspaceNativeConfirmedBatch.Execute(' in ui and
        'Continue to Classic review' in ui,
    'no imported state by checkbox': 'DataGridSelectionMode.Extended' in ui and
        'not an import approval' in ui,
    'compact current cover': 'headlineCover' in ui and
        'headlineArtFallback' in ui,
    'comparison above gallery': ui.index('POSSIBLE FIELD CHANGES') < ui.index('Artwork · current and suggested'),
    'suggested art collapsed': 'IsExpanded = false' in ui,
    'extra local artwork decoded only when expanded': 'galleryOpen && ShowExisting(existingIcon' in ui and
        'galleryOpen && ShowExisting(existingBackground' in ui and 'artworkExpander.Expanded' in ui,
    'one cover decoder reused for gallery': 'existingCover.Source = galleryOpen ? headlineCover.Source : null' in ui,
    'no passive provider requests': 'new IgdbReadOnly(' not in ui,
    'field hints strictly potential': 'Candidate may have it · verify' in ui and
        'Check live availability' in ui,
    'existing fields preserved': 'Keep existing' in ui,
    'separate rename': 'renaming is separate' in ui,
    'no guessed verified count': 'Check live availability' in ui and 'count == 1 ? " missing field"' in ui,
    'navigation respects filtered list': 'MoveFocused(int offset)' in ui and
        'table.Items.Cast<object>()' in ui,
    'previous/next buttons': 'ActionButton("Previous"' in ui and 'ActionButton("Next"' in ui,
    'still respects original classifier': 'WorkspaceTaskClassifier.Classify(state)' in ui,
    'no new database writes': all(x not in ui for x in (
        'Database.Games.Update(', 'Database.Games.Remove(', 'Database.AddFile(',
        'Database.RemoveFile(')),
    'no journal/cache mutations': all(x not in ui for x in (
        '.Commit(', '.SaveBatch(', 'File.WriteAllText(', 'File.WriteAllLines(', '.MarkUndone(')),
    'Classic entry stays independently accessible':
        'new CatalogWindow(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
    'opt-in preview menu stays intact':
        'new LibraryWorkspacePreview(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
    'release preview clearly bounded': 'PreviewLabel = "Library Workspace (development preview)"' in ui and 'Every import requires approval' in ui
        and 'No changes made' in ui,
    'loading errors fail visibly': 'could not be loaded safely' in ui,
}
failures = [label for label, passed in checks.items() if not passed]
for failure in failures:
    print('FAIL:', failure)
if failures:
    raise SystemExit(1)
print('PASS:', len(checks), 'consolidated Design C UX source checks; Windows/Playnite runtime unverified')
