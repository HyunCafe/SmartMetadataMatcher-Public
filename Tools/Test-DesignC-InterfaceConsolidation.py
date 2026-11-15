#!/usr/bin/env python3
"""v0.5.33 interface structure/selection invariants. Static, not a WPF runtime test."""
from pathlib import Path
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
service=(r/'Source/WorkspaceIdentityReview.cs').read_text(encoding='utf-8-sig')
classic=(r/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
checks={
 'settings in top-right header': 'settingsButton = ActionButton("IGDB settings", "\\uE713", ShowIgdbSettings);' in ui and 'trailing.Children.Add(settingsButton);' in ui,
 'no settings in scan commands': 'scanBar.Children.Add(settingsButton);' not in ui,
 'classic fallback remains accessible': 'ActionButton("Open Classic Matcher", "\\uE8B7", OpenClassic)' in ui,
 'history remains in header': 'trailing.Children.Add(ActionButton("History",' in ui,
 'scan controls fixed grouping': 'var scanBar = new Grid' in ui and 'var scanActions = new StackPanel' in ui,
 'scanner separate from filters': 'Grid.SetColumn(scanStatus, 1); scanBar.Children.Add(scanStatus);' in ui and 'var filterRow = new Grid' in ui,
 'status line no redundant banner': 'noticeFrame.Visibility = Visibility.Collapsed;' in ui,
 'settings still encrypted': 'new CatalogCredentialStore(dataPath).Save(clientId.Text, clientSecret.Password)' in ui,
 'connection test still present': 'WorkspaceCatalogLookup.TestConnection(id, secret, testToken.Token)' in ui,
 'existing scan still reachable': 'scanPendingButton = ActionButton("Scan next 25 pending"' in ui,
 'existing refresh reachable': 'refreshMarkedButton = ActionButton("Refresh marked saved"' in ui,
 'stop reachable': 'scanCancellation.Cancel()' in ui,
 'dialog uses structured columns': all(('AddColumn(results, '+x) in ui for x in ['"IGDB game"','"Platform(s)"','"Year"','"IGDB ID"','"Title relevance"']),
 'explicit dark candidate grid headers': 'results.ColumnHeaderStyle = choiceHeaderStyle;' in ui and 'choiceHeaderStyle.Setters.Add(new Setter(Control.BackgroundProperty, PanelRaised));' in ui,
 'candidate chooser not one long label': 'results.DisplayMemberPath = "Description";' not in ui and 'var results = new DataGrid' in ui,
 'results are relevance first': '.OrderByDescending(x => x.Rank).ThenByDescending(x => x.Platform)' in ui,
 'exact title rank': 'String.Equals(a, b, StringComparison.OrdinalIgnoreCase)' in ui,
 'partial title distinguishable': '"Partial title" : "Other result"' in ui,
 'platform relevance uses shared normalizer': 'CatalogCore.PlatformKey(platform ?? "")' in ui and 'CatalogCore.PlatformKey(value ?? "")' in ui,
 'never assume matching eligibility': 'Presentation order only.' in ui and 'not matching evidence' in ui,
 'never preselect fuzzy result': 'results.SelectedItem = null; // Never preselect an unrelated fuzzy result.' in ui,
 'explicit use button': 'ActionButton("Use highlighted suggestion"' in ui and 'results.SelectedItem as IdentityChoiceRow' in ui,
 'live exact verification still mandatory': 'service.FetchExact(igdbId' in ui and 'service.StageChoice(live' in ui,
 'identity can be forgotten separately': 'ActionButton("Forget confirmation"' in ui,
 'undo and writer remain in Classic': 'classic.StageVerifiedNativeReview(verifiedReview);' in ui and 'private void RunBatch(' in classic,
 'no UI metadata or cache writes': all(x not in ui for x in ('Database.Games.Update(', 'new IgdbReadOnly(', '.SaveBatch(', '.Commit(', 'RunBatch(', 'File.WriteAllBytes(')),
 'no write service changes': 'confirmations.Forget(live.Id);' in service and 'cache.SaveBatch(new [] {record}, DateTime.UtcNow)' in service,
}
for k,v in checks.items():
 if not v: print('FAIL:',k)
if not all(checks.values()):raise SystemExit(1)
print('PASS:',len(checks),'Design C interface consolidation and identity-dialog safeguards; Windows/Playnite unverified')
