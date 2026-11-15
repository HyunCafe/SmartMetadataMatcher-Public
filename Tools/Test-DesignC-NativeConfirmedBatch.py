#!/usr/bin/env python3
"""v0.5.47 confirmed-batch import source safety invariants; read-only."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
service=(root/'Source/WorkspaceNativeConfirmedBatch.cs').read_text(encoding='utf-8-sig')
single=(root/'Source/WorkspaceNativeSingleImport.cs').read_text(encoding='utf-8-sig')
shared=(root/'Source/WorkspaceImportCoordinator.cs').read_text(encoding='utf-8-sig')
project=(root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
checks={
 'compiled exactly once':project.count('<Compile Include="Source/WorkspaceNativeConfirmedBatch.cs" />')==1,
 'contextual verified-batch action':'ActionButton("Apply selected games..."' in ui and
    'decisionActions.Children.Add(chosen.Count == 1 ? nativeApplyButton : nativeBatchButton);' in ui,
 'existing single path untouched':ui.count('WorkspaceNativeSingleImport.Execute(')==1 and
    'StartNativeSingleImport(chosen,plan,dialog,liveStatus,continueButton,' in ui,
 'Classic fallback preserved':'Continue to Classic review' in ui and 'StageTransferredDrafts(transferDrafts)' in ui,
 'native batch selection requires confirmed or fresh strict IDs':'chosen.Count < 2' in ui and
    'chosen.All(x=>NativeBatchReady(x))' in ui and 'row.IdentityConfirmed || FreshStrictReady(row,ScanInput(game))' in ui,
 'fresh strict proof rechecked after approval':'entry.FreshStrict' in ui and
    '!FreshStrictReady(selected,current)' in ui,
 'strict executor uses original strict resolver':'CatalogCore.CanAutoPlan(resolved)' in single and
    'CatalogWindow.ResolveWithExactFallback(approvedIdentity,matches,client,token)' in single,
 'fresh detailed full selection': 'WorkspaceBatchLivePreflight.DetailedPlanReady(plan,verifiedTargets,verifiedDetails)' in ui,
 'persisted human confirmation checked':ui.count('TryMatch(current.Id,current.Name,current.Platform,')>=3,
 'current IGDB cache rechecked':'ConfirmUnchangedLookup(current,selected.CachedCatalogId,selected.CachedCatalogName);' in ui,
 'drafts same as preflight': 'WorkspaceBatchLivePreflight.SameDraft(target.Draft,' in ui,
 'full reviewed contract frozen':'frozen=WorkspaceReviewContract.Capture(plan,verifiedTargets,verifiedDetails);' in ui,
 'planned title checked':'frozen.EnsureSamePlan(approved.Select(x=>Tuple.Create(x.GameId,x.IgdbId,' in ui,
 'whole library snapshot rechecked':'frozen.EnsureCurrent(id=>' in ui,
 'all current metadata captured':'WorkspaceNativeApprovedSnapshot.Capture(api.Database.Games[selected.GameId])' in ui,
 'before and after approval checks':ui.count('frozen.EnsureSamePlan(approved.Select(')==2,
 'one batch final review':'Final Design C import approval — verified batch' in ui,
 'review all field rows':'fields.ItemsSource=plan.SelectMany(x=>verifiedDetails[x.GameId]).ToList();' in ui,
 'exact Playnite title included':'AddColumn(fields,"Current Playnite value","Current"' in ui and
    'AddColumn(fields,"Proposed handling","PlannedAction"' in ui,
 'backup acknowledgment initially unchecked':'I verified a restorable backup of my Playnite library AND extension data.' in ui and
    'var apply=ActionButton("Apply "+approved.Count+" verified games"' in ui and 'apply.IsEnabled=false;' in ui,
 'approval only after checkbox':'backup.Checked+=(s,e)=>apply.IsEnabled=true;' in ui and
    'if (confirm.ShowDialog()!=true) return;' in ui,
 'explicit partial write warning':'NOT an atomic all-or-nothing batch' in ui,
 'background run and safe progress':'Task.Run(()=>{' in ui and
    'WorkspaceNativeConfirmedBatch.Execute(api,dataPath,frozen,ScanInput,' in ui,
 'cancel active batch':'if (nativeImportCancellation != null) nativeImportCancellation.Cancel();' in ui,
 'no concurrent imports':'if (nativeApplying || checkingBatchFields || checkingLiveFields || scanning ||' in ui,
 'clear stale authorizations':'verifiedTargets.Clear();verifiedDetails.Clear();' in ui,
 'processed marks removed':'foreach(var done in result.ProcessedGameIds)markedIds.Remove(done);' in ui,
 'no extra writer in UI':'CatalogEnrichmentWriter.ApplyOne(' not in ui and 'Database.Games.Update(' not in ui,
 'batch size bounded by established resource policy':'entries.Count>WorkspaceReviewHandoff.MaximumSelectedGames' in service,
 'batch never defaults to five':'SupervisedLimit' not in service and 'entries.Count<2' in service,
 'unique Playnite record IDs':'entries.Select(x=>x.GameId).Distinct().Count()!=entries.Count' in service,
 'contract exact batch scope':'reviewed.Count!=entries.Count' in service and
    'reviewed.EnsureSamePlan(entries.Select(' in service,
 'sequential verified single writer only':service.count('WorkspaceNativeSingleImport.Execute(')==1 and
    service.count('WorkspaceImportCoordinator.ApplyOne(')==0,
 'same one existing metadata writer':shared.count('CatalogEnrichmentWriter.ApplyOne(')==1 and
    single.count('WorkspaceImportCoordinator.ApplyOne(')==1,
 'one fresh provider fetch per game':'client.FetchById(catalogId,token)' in single,
 'fresh strict route and original confirmed route':service.count('WorkspaceNativeSingleImport.ExecuteStrict(')==1 and
    service.count('WorkspaceNativeSingleImport.Execute(')==1,
 'live confirm at both boundaries':single.count('RequireConfirmed(dataPath,current,catalogId,catalogTitle);')==2,
 'playnite UI thread for mutations':single.count('dispatcher.Invoke(new Action(() => {')==2,
 'stop at first failure': 'result.Failed=1;' in service and 'result.Failure=entry.Snapshot.Name+": "+ex.Message;' in service and
    'break;' in service,
 'cancel between items':'if (cancellation.IsCancellationRequested)' in service and 'catch (OperationCanceledException)' in service,
 'no unjournaled writes': all(s not in service for s in ('Database.Games.Update(', 'journal.Prepare(',
   'journal.Commit(', 'File.WriteAllBytes(', 'MediaStaging.Begin(')),
 'partial results explicit':'result.ProcessedGameIds.Add(entry.GameId);' in service and 'result.Skipped++' in service and
    'result.Applied++' in service,
 'status report stops failed/no automatic retry':'result.Failure' in ui and 'Inspect History/Recovery before another import.' in ui,
 'fatal setup does not hang UI':'WorkspaceNativeBatchResult.Fatal(approved.Count,ex.Message)' in ui,
 'recovery/journals handled only by existing importer':'new EnrichmentJournal(dataPath);' in single and 'new EnrichmentJournal(' not in service,
}
for k,v in checks.items():
 if not v:raise AssertionError('FAIL v0.5.47: '+k)
print('PASS:',len(checks),'native confirmed batch safety assertions; Windows/Playnite runtime unverified')
