#!/usr/bin/env python3
"""v0.5.60 source guards for console mapping and library triage facets."""
from pathlib import Path
import re
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
core=(r/'Source/CatalogCore.cs').read_text(encoding='utf-8-sig')
ret=(r/'Source/WorkspaceReturnState.cs').read_text(encoding='utf-8-sig')
unit=(r/'Tests/CatalogCoreTests.cs').read_text(encoding='utf-8-sig')
# Version alignment is a live contract, not pinned to the version when this test was first written.
manifest_version = re.search(r"(?m)^Version: (\d+\.\d+\.\d+)\s*$",
    (r/'extension.yaml').read_text())
plugin_source = (r/'Source/Plugin.cs').read_text()
version_aligned = False
if manifest_version:
    v = manifest_version.group(1)
    version_aligned = tuple(map(int, v.split('.'))) >= (0, 5, 60) and all(
        f'{attribute}("{v}.0")' in plugin_source for attribute in ('AssemblyVersion', 'AssemblyFileVersion'))
checks={
 'source loaded only from Playnite': 'Source = game.Source == null ? "" : (game.Source.Name ?? "").Trim()' in ui,
 'separate source value not IGDB identity': 'Source = game.Source' in ui and 'Platform = input.Platform' in ui,
 'one filter toolbar row': 'private const string SourceFacetPrefix = "Source: ";' in ui and 'new Grid {Height = ToolbarHeight' in ui,
 'source facets added to existing platform filter': 'platformFilter.Items.Add(SourceFacetPrefix + source);' in ui,
 'source filter exact and case-insensitive': 'String.Equals(row.Source,chosen.Substring(SourceFacetPrefix.Length)' in ui,
 'platform filters retained': 'String.Equals(row.Platform,chosen,StringComparison.OrdinalIgnoreCase)' in ui,
 'source filter display only': 'private static bool MatchesPlatformOrSource' in ui and 'CatalogInput.Platform' not in ui.split('private static bool MatchesPlatformOrSource')[1].split('private void ApplyFilter()')[0],
 'source also searchable': '(row.Source ?? "")' in ui,
 'inspector exposes source': '"  ·  Source: " + row.Source' in ui,
 'no extra source table column': 'AddColumn(table, "Source"' not in ui,
 'meaningful missing scope': '"Meaningful gaps (not Series-only)"' in ui and 'VisibleInMissingScope(row.MissingMask, 1)' in ui,
 'optional Series only scope': '"Series-only gaps"' in ui and 'VisibleInMissingScope(row.MissingMask, 2)' in ui,
 'no-gap entries not meaningful': 'row.MissingMask == 0 ||' in ui,
 'series only queued last, still eligible': '.OrderBy(x => CatalogEnrichmentNeeds.Priority(x.MissingMask))' in ui and '.ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)' in ui,
 'existing scan selection intact': 'WorkspaceScanSelection.Choose(pending, markedIds)' in ui,
 'new view filters survive return': 'missingFilterIndex>5' in ret and 'MissingFilterIndex=missingFilterIndex;' in ret,
 'no source trust in resolver': 'SourceFacetPrefix' not in core and 'SourceFacetPrefix' not in (r/'Source/CatalogSmartResolver.cs').read_text(),
 'platform aliases bounded by exact keys': 'switch(x)' in core and 'default: return x;' in core and 'default: return 0;' in core,
 'console test refuses mixed platforms': '"GameCube / Wii"' in unit and '"PS3, PC (Windows)"' in unit,
 'no second provider/writer': all(v not in ui for v in ('new IgdbReadOnly(', 'api.Database.Games.Update(', 'CatalogEnrichmentWriter.ApplyOne(', 'CatalogEnrichmentWriter.UndoLast(')),
 'version and source tests aligned': version_aligned,
 'existing tested actions preserved': 'WorkspaceHistoryUndo.UndoSelected' in ui and 'WorkspaceNativeConfirmedBatch' in ui and 'ShowIgnoredGames()' in ui,
}
failed=[name for name,yes in checks.items() if not yes]
if failed:raise SystemExit('FAIL: '+', '.join(failed))
print('PASS:',len(checks),'v0.5.60 source/identity/facet guards; Windows/Playnite unverified')
