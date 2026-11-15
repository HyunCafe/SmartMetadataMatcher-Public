#!/usr/bin/env python3
"""Design C v0.5.35 read-only live field preview static invariants.

Windows compilation and Playnite runtime acceptance remain mandatory.
"""
from pathlib import Path
p=Path(__file__).resolve().parents[1]
ui=(p/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
policy=(p/'Source/WorkspaceLiveComparison.cs').read_text(encoding='utf-8-sig')
tests=(p/'Tests/WorkspaceLiveComparisonTests.cs').read_text(encoding='utf-8-sig')
checks={
  'inspector uses exact ID fetch': 'new WorkspaceIdentityReview(dataPath).FetchExact(igdbId,' in ui,
  'live Playnite preflight': 'input = CurrentIdentityInput(selected);' in ui,
  'pre-and-post cache checks': ui.count('ConfirmUnchangedLookup(')>=3,
  'cache match compares immutable ID/title': 'WorkspaceIdentityRules.SameSaved(record, id, title, live)' in ui,
  'async off WPF thread': 'Task.Run(() => {' in ui,
  'no implicit automatic lookups': 'checkLiveFieldsButton = ActionButton(' in ui,
  'refresh count preserves existing regression': 'refreshMarkedButton.IsEnabled = !scanning && savedMarkedCount > 0;' in ui,
  'live-field lookup disables marked refresh': 'if (checkingLiveFields) refreshMarkedButton.IsEnabled = false;' in ui,
  'user can cancel field lookup': '"Cancel field check"' in ui and 'liveFieldCancellation.Cancel();' in ui,
  'window close cancels field fetch': 'if (liveFieldCancellation != null) liveFieldCancellation.Cancel();' in ui,
  'in-memory only': 'private WorkspaceLiveComparisonResult liveFieldResult;' in ui,
  'read-only preview resets on reload': 'liveFieldResult = null; // Revalidate data and identity' in ui,
  'no hidden identity confirmation': 'WorkspaceLiveComparison.Build(current, exact,' in ui,
  'same field list as preview': 'fieldComparison.ItemsSource = hasLiveFields' in ui,
  'no duplicate import writer': all(s not in ui for s in ('Database.Games.Update(', 'RunBatch(', 'new IgdbReadOnly(', '.SaveBatch(')),
  'policy validates exact ID': 'exact.Id != expectedId' in policy,
  'policy validates exact title': 'String.Equals(exact.Name, expectedTitle, StringComparison.Ordinal)' in policy,
  'policy compares missing mask': 'current.MissingCount != CatalogEnrichmentNeeds.Count(current.MissingMask)' in policy,
  'policy preserves existing values': 'if (!missing && name != "Cover") return;' in policy,
  'image availability is not download proof': 'verify JPEG download on import' in policy,
  'valid image ID check': 'MetadataPlan.SafeImageId' in policy,
  'release years are platform specific': 'x.PlatformId == platformId' in policy,
  'uses existing platform mapper': 'CatalogCore.PlatformId(current.Platform)' in policy,
  'generic release year never used': 'exact.FirstReleaseYear' not in policy,
  'series genres companies included': all('"'+s+'"' in policy for s in ['Series','Genres','Developers','Publishers']),
  'title remains unchanged': 'Keep existing · renaming requires separate approval' in policy,
  'no policy-side file or data writes': all(s not in policy for s in ['File.Write','SaveBatch(','Database.Games.Update(','ConfirmMany(','new MetadataJournal(']),
  'unit tests release port mismatch': 'wrong-platform release year' in tests,
  'unit tests exact title mismatch': 'changed exact title rejected' in tests,
  'unit tests preserve existing': 'existing cover retained' in tests,
  'unit tests malformed field mask': 'inconsistent missing field mask' in tests,
  'tests compiled in runner': 'WorkspaceLiveComparisonTests.Run();' in (p/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig'),
  'source compiled in plugin': 'Source/WorkspaceLiveComparison.cs' in (p/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig'),
  'source compiled in offline tests': '../Source/WorkspaceLiveComparison.cs' in (p/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig'),
}
for label,ok in checks.items():
 if not ok:print('FAIL:',label)
if not all(checks.values()):raise SystemExit(1)
print('PASS:',len(checks),'native live-field availability static assertions; Windows/Playnite runtime unverified')
