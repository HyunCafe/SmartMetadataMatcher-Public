#!/usr/bin/env python3
"""Read-only source-invariant checks for Design C preview. Not UI automation."""
from pathlib import Path
import re

root = Path(__file__).resolve().parent.parent
ui = (root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
plugin = (root/'Source/Plugin.cs').read_text(encoding='utf-8-sig')
checks = {
 'extended WPF row selection': 'SelectionMode = DataGridSelectionMode.Extended' in ui,
 'explicit Mark column': 'Header = "Mark"' in ui and 'Binding("Marked")' in ui,
 'marking limited to actionable games': 'public bool CanMark' in ui and 'WorkspaceTaskGroup.Updated' not in ui.split('public bool CanMark')[1].split('public bool Marked')[0],
 'marking separate from highlighting': 'private readonly HashSet<Guid> markedIds' in ui and 'table.SelectedItems.OfType<LibraryWorkspaceItem>()' in ui,
 'marking re-applied by GameId on reload': 'item.GameId)' in ui and 'item.Marked = item.CanMark && markedIds.Contains(item.GameId)' in ui,
 'context menu marks': 'new ContextMenu()' in ui and 'Mark highlighted in preview' in ui,
 'explicit, not silent handoff to Classic': 'new WorkspaceReviewHandoff(chosen.Select(x => x.GameId))' in ui and
     'Continue to Classic review' in ui and (
  'var classic = new CatalogWindow(api, dataPath, handoff);' in ui and
  'classic.StageVerifiedNativeReview(verifiedReview);' in ui and
  'classic.Show();' in ui and
  ui.index('var classic = new CatalogWindow(api, dataPath, handoff);') < ui.index('classic.StageVerifiedNativeReview(verifiedReview);') < ui.index('classic.Show();')
),
 'local image lookup': 'api.Database.GetFullFilePath(imageId)' in ui,
 'local artwork only from absolute disk paths': 'Path.IsPathRooted(path)' in ui and 'File.Exists(path)' in ui,
 'bounded local artwork decode': 'DecodePixelWidth = decodeWidth' in ui and '24 * 1024 * 1024' in ui,
 'opt-in suggested artwork': 'Preview suggested artwork (online)' in ui and 'LoadSuggestedArtwork' in ui,
 'fixed IGDB image host': 'https://images.igdb.com/igdb/image/upload/' in ui and 'ValidImageId' in ui,
 'search clear': 'searchClearButton' in ui and 'Clear search' in ui and 'searchClearButton.Click' in ui,
 'platform and missing-field filters': 'platformFilter.SelectionChanged' in ui and 'missingFilter.SelectionChanged' in ui,
 'category shortcuts': 'NavigationTab(' in ui,
 'keyboard shortcuts': 'Key.F5' in ui and 'Key.F' in ui and 'Key.Escape' in ui,
 'scan preview chrome': 'PreviewLabel = "Library Workspace (development preview)"' in ui and 'Every import requires approval' in ui and 'No changes made' in ui,
 'Classic menu preserved': 'new CatalogWindow(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
 'preview menu preserved': 'new LibraryWorkspacePreview(PlayniteApi, GetPluginUserDataPath()).Show()' in plugin,
 'no Playnite database writes': not any(x in ui for x in ('Database.Games.Update(', 'Database.RemoveFile(', 'Database.AddFile(')),
 'no new IGDB client calls': 'new IgdbReadOnly(' not in ui,
 'no user-data or journal writes': not any(x in ui for x in ('.SaveBatch(', 'File.WriteAllText(', 'File.WriteAllLines(', '.Commit(', '.MarkUndone(')),
 'classifier remains presentation-only': 'WorkspaceTaskClassifier.Classify(state)' in ui,
}
failed = [name for name, condition in checks.items() if not condition]
for name in failed:
 print('FAIL:',name)
if failed:
 raise SystemExit(1)
print('PASS:',len(checks),'read-only Design C UX source-invariant checks; not a Playnite runtime certification.')
