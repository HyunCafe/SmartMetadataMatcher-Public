#!/usr/bin/env python3
"""Read-only source guards for v0.5.51 ephemeral strict evidence; not runtime proof."""
from pathlib import Path
import re
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
plan=(root/'Source/WorkspaceBatchReview.cs').read_text(encoding='utf-8-sig')
service=(root/'Source/WorkspaceFreshStrictLookup.cs').read_text(encoding='utf-8-sig')
checks={
 'session-only evidence dictionary':'private readonly Dictionary<Guid,FreshStrictEvidence> freshStrictEvidence' in ui,
 'exact game identity':all(k in ui for k in ('Snapshot.Id == current.Id','Snapshot.Id != Guid.Empty','Snapshot.MissingMask == current.MissingMask','Snapshot.LocalReleaseYear == current.LocalReleaseYear')),
 'exact original name/platform':all(k in ui for k in ('String.Equals(Snapshot.Name,current.Name,StringComparison.Ordinal)','String.Equals(Snapshot.Platform,current.Platform,StringComparison.Ordinal)')),
 'exact catalog title/id':all(k in ui for k in ('value == IgdbId','String.Equals(CatalogTitle,title,StringComparison.Ordinal)')),
 'cache version invalidation':'String.Equals(CacheTimestamp,timestamp,StringComparison.Ordinal)' in ui,
 'saved result required':'!row.FromSaved' in ui,
 'typed proof in preview':'StrictEvidence=FreshStrictReady(x,input)' in ui,
 'fresh status only for proof':'MatchStatus=FreshStrictReady(x,input)?"Strong candidate":x.CachedMatchStatus' in ui,
 'planner DTO explicit field':'internal bool StrictEvidence;' in plan,
 'planner consumes typed proof':'StrictEvidence = x.StrictEvidence' in plan,
 'original saved-score bypass removed':'StrictEvidence = false' not in plan,
 'distinct strict UI state':'Fresh strict evidence · verify exact ID' in plan,
 'explicit action':'Verify fresh strict readiness for marked games (read-only)' in ui,
 'unconfirmed saved marks only':'x.FromSaved && !x.IdentityConfirmed' in ui,
 'visible consent':'Verify fresh strict evidence (read-only)' in ui and 'MessageBoxButton.YesNo' in ui,
 'original bounded resolver':'CatalogWindow.ResolveWithExactFallback(input,matches,client,token)' in service,
 'official strict gate':'CatalogCore.CanAutoPlan(resolved)' in service,
 'same IGDB record as saved':'resolved.Game.Id==request.Item2' in service and 'String.Equals(resolved.Game.Name,request.Item3,StringComparison.Ordinal)' in service,
 'searches fully fresh':'client.SearchVariants(variants.Select(x=>x.Text),token)' in service,
 'cancellable':'scanCancellation=new CancellationTokenSource();' in ui and 'token.ThrowIfCancellationRequested();' in ui,
 'rate paced':'token.WaitHandle.WaitOne(350)' in service,
 'revalidate cache after worker':'String.Equals(saved.ScannedUtc,proof.CacheTimestamp,StringComparison.Ordinal)' in ui,
 'revalidate metadata after worker':'var current=ScanInput(liveGame);' in ui,
 'no persisting proof':'freshStrictEvidence[current.Id]=proof;' in ui and 'new CatalogScanRecord' not in ui.split('private void VerifyFreshStrictEvidence()',1)[1].split('private void StartFullRescan()',1)[0],
 'invalidation after rescan':'freshStrictEvidence.Clear();' in ui,
 'invalidation after confirmation':'freshStrictEvidence.Remove(item.GameId);' in ui,
 'invalidation after refresh':'foreach(var input in inputs) freshStrictEvidence.Remove(input.Id);' in ui,
 'invalidation when reload differs':'if (savedRow==null || !knownGames.TryGetValue(key,out currentGame) ||' in ui,
 'existing native importer':'WorkspaceNativeConfirmedBatch.Execute(' in ui and 'WorkspaceNativeSingleImport.Execute(' in ui,
 'Classic fallback preserved':'Continue to Classic review' in ui,
}
method=ui.split('private void VerifyFreshStrictEvidence()',1)[1].split('private void StartFullRescan()',1)[0]
for name,phrase in {'no direct Playnite writes':'api.Database.Games.Update(',
 'no duplicate writer':'CatalogEnrichmentWriter.ApplyOne(',
 'no journal mutations':'EnrichmentJournal(',
 'no implicit identity confirmation':'review.Confirm(',
 'no native import':'WorkspaceNativeConfirmedBatch.Execute('}.items():
 checks[name]=phrase not in method
checks['consent before network']=method.index('if (answer!=MessageBoxResult.Yes) return;')<method.index('Task.Run(')
checks['provider isolated from WPF']='new IgdbReadOnly(' not in ui and 'new IgdbReadOnly(' in service and 'api.Database' not in service
checks['provider result before proof publication']=method.index('WorkspaceFreshStrictLookup.Check(')<method.index('freshStrictEvidence[current.Id]=proof;')
checks['no provider-side write or confirmation']=all(v not in service for v in ('CatalogEnrichmentWriter.ApplyOne(', 'api.Database.Games.Update(', 'review.Confirm(', 'new EnrichmentJournal('))
checks['service in project']='<Compile Include="Source/WorkspaceFreshStrictLookup.cs" />' in (root/'SmartMetadataMatcher.csproj').read_text(encoding='utf-8-sig')
checks['only reviewed live statuses eligible']='StrictEvidence = x.StrictEvidence' in plan and 'FromSavedScan = x.Saved' in plan
fail=[key for key,ok in checks.items() if not ok]
for k in fail:print('FAIL:',k)
if fail:raise SystemExit(1)
print('PASS:',len(checks),'fresh resolver, cache binding, cancellation, no-write and parity assertions; Windows/Playnite unverified')
