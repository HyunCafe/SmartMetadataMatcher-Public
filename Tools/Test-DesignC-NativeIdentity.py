#!/usr/bin/env python3
"""Native identity review structural invariants; not Windows/runtime certification."""
from pathlib import Path
p=Path(__file__).resolve().parent.parent
ui=(p/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
service=(p/'Source/WorkspaceIdentityReview.cs').read_text(encoding='utf-8-sig')
tests=(p/'Tests/WorkspaceIdentityReviewTests.cs').read_text(encoding='utf-8-sig')
proj=(p/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
testproj=(p/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig')
runner=(p/'Tests/NameRulesTests.cs').read_text(encoding='utf-8-sig')
checks={
 'manual title search explicit': 'ActionButton("Search IGDB"' in ui and 'service.Search(term' in ui,
 'manual exact ID permitted': 'ActionButton("Check exact ID"' in ui and 'Int64.TryParse(idField.Text.Trim()' in ui,
 'alternative explicit selection': 'Use highlighted suggestion' in ui and 'results.SelectedItem as IdentityChoiceRow' in ui,
 'exact IGDB ID verified before stage': 'service.FetchExact(igdbId' in ui and 'service.StageChoice(live' in ui,
 'manual changes require yes no': 'Choose suggested IGDB identity", MessageBoxButton.YesNo' in ui,
 'identity confirmation explicit': 'ActionButton("Confirm saved identity"' in ui and 'StartConfirmIdentity' in ui,
 'confirmation re-fetches exact record': 'new WorkspaceIdentityReview(dataPath).FetchExact(igdbId' in ui,
 'confirmation checks exact fetched title': 'String.Equals(fetched.Name,' in ui and 'StringComparison.Ordinal' in ui,
 'confirmation requires affirmative': '"Confirm IGDB identity", MessageBoxButton.YesNo' in ui,
 'confirmation is not import approval': 'No Playnite metadata changed or import authorized' in ui,
 'forget is opt-in': 'ActionButton("Forget confirmation"' in ui and '"Forget IGDB identity", MessageBoxButton.YesNo' in ui,
 'forget hidden unless confirmed': 'row.IdentityConfirmed ?' in ui,
 'live game state rechecked': 'private CatalogInput CurrentIdentityInput' in ui and 'live.MissingMask != before.MissingMask' in ui and 'live.LocalReleaseYear != before.LocalReleaseYear' in ui,
 'old mark cleared on correction': 'markedIds.Remove(selected.GameId)' in ui,
 'cache validation before mutation': 'cache.Load(); // Fail closed' in service and 'cache.TryGet(live, DateTime.UtcNow' in service,
 'cache exact title and ID': 'String.Equals(saved.CatalogId, id, StringComparison.Ordinal)' in service and 'String.Equals(saved.Candidate, title, StringComparison.Ordinal)' in service,
 'manual record only Review': 'Status = "Review", Score = ""' in service,
 'old confirmation invalidated': 'confirmations.Forget(live.Id);' in service,
 'cache stage uses existing atomic store': 'cache.SaveBatch(new [] {record}, DateTime.UtcNow)' in service,
 'persistent confirmation only direct explicit action': 'confirmations.Confirm(live.Id, live.Name, live.Platform, exact.Id' in service,
 'deduplicated bounded search': '.GroupBy(x => x.Id).Select(x => x.First()).Take(25)' in service,
 'same IGDB library': 'new IgdbReadOnly(clientId, clientSecret)' in service,
 'same exact-ID implementation': 'client.FetchById(id, token)' in service,
 'no Playnite database writes in service': all(x not in service for x in ('Database.Games.Update(', 'RunBatch(', '.MarkUndone(', 'new MetadataJournal(', 'new EnrichmentJournal(')),
 'no network or file mutation in UI': all(x not in ui for x in ('new IgdbReadOnly(', 'Database.Games.Update(', '.SaveBatch(', 'File.WriteAllBytes(', '.Commit(')),
 'new service in plugin compile list': 'Source/WorkspaceIdentityReview.cs' in proj,
 'new service and tests in unit project': 'WorkspaceIdentityReview.cs' in testproj and 'WorkspaceIdentityReviewTests.cs' in testproj,
 'offline runner invokes tests': 'WorkspaceIdentityReviewTests.Run();' in runner,
 'offline tests include staged cache and confirmed store': 'service.StageChoice(input' in tests and 'confirmations.TryMatch(' in tests,
 'existing old fallback': 'classic.StageVerifiedNativeReview(verifiedReview);' in ui,
 'existing native scans preserved': 'new WorkspaceCatalogLookup(dataPath).Run(inputs,' in ui,
 'existing native refresh preserved': 'WorkspaceRefreshPolicy.ChooseMarked(savedIds, markedIds)' in ui,
 'no second matcher': 'CatalogWindow.ResolveWithExactFallback' not in service,
}
failed=[k for k,v in checks.items() if not v]
for f in failed:print('FAIL:',f)
if failed:raise SystemExit(1)
print('PASS:',len(checks),'native IGDB identity-review and correction source checks; Windows and Playnite unverified')
