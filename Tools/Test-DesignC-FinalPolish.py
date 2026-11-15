#!/usr/bin/env python3
"""Static Design C interaction/contrast checks. NOT a WPF or Playnite runtime test."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Source'/'LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
plugin=(root/'Source'/'Plugin.cs').read_text(encoding='utf-8-sig')
project=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')

checks={
    'text is centered vertically in grid cells':
        'style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));' in ui,
    'cell alignment explicitly centered':
        'cellStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));' in ui,
    'row alignment explicitly centered':
        'rowStyle.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));' in ui,
    'status glyph and caption explicitly centered':
        ui.count('SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center)') >= 3,
    'checkbox vertically centered':
        'check.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);' in ui,
    'consistent horizontal cell breathing room':
        'cellStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 2, 12, 2)));' in ui,
    'space retained for long platforms':
        'table.Columns[3].Width = new DataGridLength(1.30, DataGridLengthUnitType.Star)' in ui,
    'title and suggestion preserved as independent columns':
        'AddColumn(table, "Game in Playnite", "Title", 250);' in ui and
        'AddColumn(table, "IGDB suggestion", "Candidate", 190);' in ui,
    'tooltips retain full string for clipped values':
        'style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty, new Binding(property)));' in ui,
    'compact status with inspector verification':
        'case WorkspaceTaskGroup.Suggestions: return "Suggested";' in ui and
        'Live check required before applying metadata' in ui,
    'single preview guidance banner':
        'Scans save IGDB suggestions only.' in ui and
        'Saved suggestions aren\'t verified changes' not in ui,
    'specific suggestion next step':
        'row.Missing + " is missing in Playnite. Refresh this IGDB suggestion "' in ui,
    'specific alternative match next step':
        'The suggested identity needs your decision.' in ui,
    'singular/plural field grammar':
        'count == 1 ? " missing field" : " missing fields"' in ui,
    'no guessed verified field additions':
        '0 verified additions in this preview' not in ui and
        'Check live availability' in ui,
    'consistent button description':
        'ActionButton("Open Classic Matcher"' in ui and
        'Open Classic to scan / apply' not in ui,
    'unnecessary caution stripped from inspector':
        'Potential only · no verified additions' not in ui,
    'contextual selection actions':
        'bool canMark = highlightedRows.Any(x => x.CanMark && !x.Marked);' in ui and
        'bool canUnmark = highlightedRows.Any(x => x.CanMark && x.Marked);' in ui and
        'markHighlightedButton.Visibility = canMark ? Visibility.Visible : Visibility.Collapsed;' in ui and
        'unmarkHighlightedButton.Visibility = canUnmark ? Visibility.Visible : Visibility.Collapsed;' in ui,
    'mark counts and hidden counts preserved':
        'outside this view' in ui and 'markedIds.Count + " marked"' in ui,
    'review never available for zero marked':
        'reviewMarksButton.IsEnabled = hasMarks;' in ui and
        'bool hasMarks = markedIds.Count > 0;' in ui,
    'review emphasis depends on marks':
        'reviewMarksButton.Background = hasMarks ? Accent : Panel;' in ui,
    'review caption reports exact count':
        '"Review " + markedIds.Count + " marked "' in ui,
    'explicit preview selection separate from import':
        'private readonly HashSet<Guid> markedIds' in ui and
        'Continue to Classic review' in ui and
        'No data is imported until you' in ui,
    'complete classic menu remains intact':
        'new CatalogWindow(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
    'opt-in workspace remains available':
        'new LibraryWorkspacePreview(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
    'new workspace compiled':
        '<Compile Include="Source/LibraryWorkspacePreview.cs" />' in project,
    'no Playnite metadata writes':
        all(token not in ui for token in ('Database.Games.Update(', 'Database.Games.Remove(',
          'Database.AddFile(', 'Database.RemoveFile(', '.SaveBatch(', '.Commit(', '.MarkUndone(')),
    'no new live IGDB requests': 'new IgdbReadOnly(' not in ui,
    'no file-writing calls':
        all(token not in ui for token in ('File.WriteAllText(', 'File.WriteAllLines(',
          'FileStream(path, FileMode.Create')),
    'existing keyboard controls preserved':
        all(token in ui for token in ('Key.F5', 'Key.Escape', 'Key.F', 'Key.M',
          'SelectionMode = DataGridSelectionMode.Extended')),
    'field comparison above artwork':
        ui.index('POSSIBLE FIELD CHANGES') < ui.index('Artwork · current and suggested'),
}
for label,ok in checks.items():
    if not ok: print('FAIL:', label)
if not all(checks.values()): raise SystemExit(1)
print('PASS:', len(checks), 'read-only Design C final-polish invariants. Windows/Playnite behavior unverified.')
