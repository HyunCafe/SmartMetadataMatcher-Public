#!/usr/bin/env python3
"""Static checks for shared importer extraction and old writer removal."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
project=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
classic=(root/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
shared=(root/'Source/WorkspaceImportCoordinator.cs').read_text(encoding='utf-8-sig')
checks={
 'single compile registration': project.count('<Compile Include="Source/WorkspaceImportCoordinator.cs" />')==1,
 'single Classic batch entry': classic.count('WorkspaceImportCoordinator.ApplyOne(api,enrichmentJournal,')==2,
 'no Classic direct metadata writer': 'CatalogEnrichmentWriter.ApplyOne(' not in classic,
 'one physical writer handoff': shared.count('CatalogEnrichmentWriter.ApplyOne(')==1,
 'frozen review and exact live catalog check': 'catalog == null ? 0 : catalog.Id' in shared and
     'catalog == null ? null : catalog.Name' in shared and 'reviewed.EnsureOneCurrent(gameId,' in shared,
 'field-by-field writer still authoritative': 'expectedDescription, expectedCover,' in shared and 'expectedIcon, expectedBackground' in shared,
 'manual single-game path available': 'null, ScanInput, row.GameId,' in classic,
 'no second journal in coordinator': 'new EnrichmentJournal(' not in shared,
}
for label,pass_ in checks.items():
 if not pass_:raise AssertionError('FAIL v0.5.45: '+label)
print('PASS:',len(checks),'shared import coordinator extraction assertions')
