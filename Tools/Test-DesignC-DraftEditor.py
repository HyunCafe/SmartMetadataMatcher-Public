#!/usr/bin/env python3
"""v0.5.36 native Design C session-only draft interface and policy invariants.

These are source checks, not Windows compilation or a binary security audit.
"""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
policy=(root/'Source/WorkspaceDraftPolicy.cs').read_text(encoding='utf-8-sig')
tests=(root/'Tests/WorkspaceDraftPolicyTests.cs').read_text(encoding='utf-8-sig')
checks={
 'editor is an explicit user action': 'editDraftButton = ActionButton("Choose fields / custom text"' in ui,
 'no auto editor on selecting rows': 'private void EditFocusedDraft()' in ui,
 'transient game keyed state': 'readonly Dictionary<Guid, WorkspaceDraftState> draftStates' in ui,
 'independent copy per edit': 'existing.Draft.Copy()' in ui,
 'exact Playnite preflight before dialog': 'before = CurrentIdentityInput(selected);' in ui,
 'cache identity preflight': ui.count('ConfirmUnchangedLookup(')>=4,
 'live field check prerequisite': 'if (!LiveFieldPreviewMatches(selected)) return;' in ui,
 'dialog is modal for stable snapshot': 'dialog.ShowDialog() == true' in ui,
 'custom fields visible before IGDB selection': ui.index('var customTitle = new CheckBox', ui.index('private void EditFocusedDraft()')) < ui.index('foreach (var field in compared.Fields.Where', ui.index('private void EditFocusedDraft()')),
 'explicit editable limits': 'those values are not inline-editable' in ui and 'Custom title and description are editable below' in ui,
 'explicit opt-in custom description': 'draft.CustomDescriptionEnabled = customDescription.IsChecked == true;' in ui,
 'explicit opt-in custom title': 'draft.CustomTitleEnabled = customTitle.IsChecked == true;' in ui,
 'individual flags': 'WorkspaceDraftPolicy.SetIncluded(draft, option.Key,' in ui,
 'no entry for unavailable field': 'if (option.Value.IsEnabled)' in ui,
 'no auto metadata replacement': 'IsEnabled = field.MayAdd' in ui,
 'avoid dark theme color mismatch': 'Foreground = Text, FontSize = 13' in ui,
 'collision checks': 'WorkspaceDraftPolicy.Save(current, compared, draft, library)' in ui,
 'recheck Playnite after dialog': 'var current = CurrentIdentityInput(selected);' in ui,
 'recheck cache after dialog': 'ConfirmUnchangedLookup(current, selected.CachedCatalogId, selected.CachedCatalogName);' in ui,
 'session-only clear': 'draftStates.Remove(selected.GameId);' in ui,
 'invalid drafts are discarded': 'draftStates.Remove(row.GameId);' in ui,
 'field table reflects exclusions': 'WorkspaceDraftPolicy.ActionLabel(x,' in ui,
 'cannot edit during scan/refresh': 'if (scanning || checkingLiveFields || table == null) return;' in ui,
 'no import authorization claim': 'This session-only draft does NOT update' in ui,
 'no WPF Playnite writes': all(x not in ui for x in ['Database.Games.Update(', 'RunBatch(', 'new IgdbReadOnly(', '.SaveBatch(']),
 'state binds exact name/platform/id': 'Draft.Matches(live.Id, live.Name, live.Platform, id, title)' in policy,
 'state binds missing mask': 'live.MissingMask == ExpectedMissingMask' in policy,
 'state binds release snapshot': 'live.LocalReleaseYear == ExpectedReleaseYear' in policy,
 'validates exact comparison': 'WorkspaceDraftPolicy' in policy and 'CheckComparison(live, compared);' in policy,
 'uses original draft validation': 'CatalogImportDraft.Valid(proposed)' in policy,
 'uses original collision planner': 'CatalogImportDraft.ResolveTitle(' in policy,
 'clones saved proposal': 'Draft = proposed.Copy()' in policy,
 'only nine supported flags': '"Release date"' in policy and '"Series"' in policy,
 'opt-out action label': 'Excluded from this draft' in policy,
 'no persistence or metadata mutation in policy': all(x not in policy for x in ['File.Write', 'SaveBatch(', 'Database.Games.Update(', 'ConfirmMany(', 'new MetadataJournal(', 'new EnrichmentJournal(']),
 'tests changed identity': 'changed IGDB ID invalidates draft' in tests,
 'tests changed metadata': 'changed missing fields invalidate draft' in tests,
 'tests defensive copy': 'stored draft is a defensive clone' in tests,
 'tests collisions': 'colliding title cannot be staged' in tests,
 'tests description validation': 'empty custom description rejected' in tests,
 'tests cover preservation': 'existing cover cannot be selected for replacement' in tests,
 'test runner wired': 'WorkspaceDraftPolicyTests.Run();' in (root/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig'),
 'plugin project wired': 'Source/WorkspaceDraftPolicy.cs' in (root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig'),
 'offline project wired': '../Source/WorkspaceDraftPolicy.cs' in (root/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig'),
}
for label,ok in checks.items():
 if not ok: print('FAIL:',label)
if not all(checks.values()):raise SystemExit(1)
print('PASS:',len(checks),'Design C draft editing/identity safety checks; Windows compilation and Playnite runtime pending')
