#!/usr/bin/env python3
"""v0.5.46 single-game native import safety and regression invariants (read-only)."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
service=(root/'Source/WorkspaceNativeSingleImport.cs').read_text(encoding='utf-8-sig')
shared=(root/'Source/WorkspaceImportCoordinator.cs').read_text(encoding='utf-8-sig')
project=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
assertions={
 'single native implementation compiled once':project.count('<Compile Include="Source/WorkspaceNativeSingleImport.cs" />')==1,
 'single game only': 'chosen.Count!=1 || plan.Count!=1' in ui and 'plan.Count==1' in ui,
 'exact field review required': 'WorkspaceBatchLivePreflight.DetailedPlanReady(plan,verifiedTargets,verifiedDetails)' in ui,
 'explicit identity confirmed': 'if (!selected.IdentityConfirmed)' in ui,
 'live confirmed store revalidation':ui.count('new CatalogConfirmedIdentities(dataPath)')>=2 and 'identities.TryMatch(current.Id,current.Name,current.Platform,' in ui,
 'read only previous state': 'WorkspaceReviewContract.Capture(plan,verifiedTargets,verifiedDetails)' in ui,
 'verify frozen plan': 'frozen.EnsureSamePlan(new[] {Tuple.Create(' in ui,
 'verify immediately after approval': 'if (confirm.ShowDialog()!=true) return;' in ui and 'frozen.EnsureCurrent(id =>' in ui,
 'separate final approval': 'Final Design C import approval — one game' in ui,
 'backup acknowledgement starts off': 'I verified a restorable backup of my Playnite library AND extension data.' in ui and 'approve.IsEnabled=false;' in ui,
 'backup unlocks apply only': 'backup.Checked+=(s,e)=>approve.IsEnabled=true;' in ui,
 'exact title prominently visible': 'Display title after import:' in ui,
 'itemized final fields': 'fields.ItemsSource=verifiedDetails[selected.GameId].ToList();' in ui,
 'no second writer in UI': 'CatalogEnrichmentWriter.ApplyOne(' not in ui and 'Database.Games.Update(' not in ui,
 'one native import service route':ui.count('WorkspaceNativeSingleImport.Execute(')==1,
 'no competing Classic writer': shared.count('CatalogEnrichmentWriter.ApplyOne(')==1,
 'same shared import coordinator': 'WorkspaceImportCoordinator.ApplyOne(api,journal,review,liveSnapshot,' in service,
 'live snapshot before fetching': 'dispatcher.Invoke(new Action(() => {' in service,
 'same snapshot before write':service.count('review.EnsureOneCurrent(gameId,current,catalogId,catalogTitle,approvedTitle,draft);')==2,
 'confirm identity at both boundaries':service.count('RequireConfirmed(dataPath,current,catalogId,catalogTitle);')==2,
 'provider fetch is exact': 'exact = client.FetchById(catalogId,token);' in service,
 'provider ID and title strict': 'exact.Id != catalogId' in service and 'String.Equals(exact.Name,catalogTitle,StringComparison.Ordinal)' in service,
 'provider console strict': 'exact.PlatformIds.Contains(CatalogCore.PlatformId(approved.Platform))' in service,
 'Playnite database writes via dispatcher':service.count('dispatcher.Invoke(new Action(() => {')==2,
 'existing journal only': 'var journal = new EnrichmentJournal(dataPath);' in service and 'new MetadataJournal(' not in service,
 'current metadata field snapshot': 'GameExtrasSnapshot.Capture(game)' in service,
 'existing metadata preserves': 'MetadataPlan.Missing(approved.Cover)' in service and 'MetadataPlan.Missing(approved.Icon)' in service,
 'existing user draft choices': '(draft == null || draft.Cover)' in service and '(draft == null || draft.Background)' in service,
 'title cleaning on approved imports': 'true,approved.Extras,approvedTitle,draft);' in service,
 'cancel before write': 'token.ThrowIfCancellationRequested();' in service,
 'no direct mutation':all(bad not in service for bad in ('Database.Games.Update(', 'journal.Prepare(', 'journal.Commit(', 'MediaStaging.Begin(', 'File.WriteAllBytes(')),
 'cancel safety for both modal & main':ui.count('if (nativeImportCancellation != null) nativeImportCancellation.Cancel();')>=2,
 'no concurrent native apply': 'if (nativeApplying || checkingBatchFields || chosen.Count!=1 || plan.Count!=1)' in ui,
 'prevent stale approval replay': 'verifiedTargets.Clear();verifiedDetails.Clear();' in ui,
 'Classic remains available for multi game': 'Continue to Classic review' in ui and 'new CatalogWindow(api, dataPath, handoff)' in ui,
 'unverified preview remains labeled': 'development preview' in ui,
}
for label,ok in assertions.items():
 if not ok:raise AssertionError('FAIL v0.5.46: '+label)
print('PASS:',len(assertions),'native single-import safety assertions; Playnite runtime not verified')
