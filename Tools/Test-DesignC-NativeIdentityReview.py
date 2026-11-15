#!/usr/bin/env python3
"""v0.5.48 native reviewed-identity flow invariants (read-only source tests).

Source checks supplement, but do not replace, Windows compilation and live Playnite
checks of IGDB fetch, confirmation persistence, cancellation, and import/undo.
"""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
ui=(root/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
batch=(root/'Source/WorkspaceNativeConfirmedBatch.cs').read_text(encoding='utf-8-sig')
single=(root/'Source/WorkspaceNativeSingleImport.cs').read_text(encoding='utf-8-sig')
shared=(root/'Source/WorkspaceImportCoordinator.cs').read_text(encoding='utf-8-sig')
old_help='Multi-game reviews continue through Classic; confirmed identities can also be imported in one native batch.'
method=ui.split('private void ConfirmMarkedIdentities(',1)[-1].split('private void StartNativeConfirmedBatch(',1)[0]
checks={
 'new button explicitly labeled': 'Review unconfirmed identities...' in ui,
 'only one implementation':ui.count('private void ConfirmMarkedIdentities(')==1,
 'button calls native method':ui.count('ConfirmMarkedIdentities(chosen,plan,dialog,grid,liveStatus,')==1,
 'old workflow help removed':old_help not in ui,
 'confirmation is per unconfirmed item':'chosen.Where(x => !x.IdentityConfirmed).ToList()' in method,
 'no duplicate identities promoted':'if (pending.Count == 0)' in method,
 'fresh read-only provider fetch':'review.FetchExact(entry.Item2,credentials.ClientId,' in method,
 'fetch checks cancellation':'credentials.ClientSecret,cancellation.Token)' in method,
 'no implicit yes': 'MessageBoxButton.YesNo' in method and 'answer != MessageBoxResult.Yes' in method,
 'exact catalog id':'exact.Id != entry.Item2' in method,
 'exact title':'String.Equals(exact.Name,entry.Item3,StringComparison.Ordinal)' in method,
 'platform expected structured id':'CatalogCore.PlatformId(current.Platform)' in method,
 'IGDB platforms checked':'!exact.PlatformIds.Contains(platformId)' in method,
 'cache snapshot rechecked':'ConfirmUnchangedLookup(current,entry.Item2.ToString(' in method,
 'user decisions saved with existing service':'review.Confirm(current,item.CachedCatalogId,item.CachedCatalogName,exact);' in method,
 'no import from identity review':'WorkspaceNativeSingleImport.Execute(' not in method and 'WorkspaceNativeConfirmedBatch.Execute(' not in method,
 'never call original writer in review':'CatalogEnrichmentWriter.ApplyOne(' not in method,
 'invalidates cached field data':'verifiedTargets.Clear(); verifiedDetails.Clear();' in method,
 'stops stale field plan':'row.LiveCheck="Not checked";' in method and 'row.AvailableFields="—";' in method,
 'approval disabled':'nativeBatchButton.IsEnabled=false;' in method and 'singleButton.IsEnabled=false;' in method,
 'explicit fresh field check':'"Confirmed "+saved+" game(s). Run Check exact fields for the complete batch."' in method,
 'current data fetched':'CurrentIdentityInput(item)' in method,
 'stale game aborts':'CurrentIdentityInput(item);' in method and 'ConfirmUnchangedLookup(input,item.CachedCatalogId,item.CachedCatalogName)' in method,
 'UI thread provider async':'Task.Run(() => {' in method and 'dialog.Dispatcher.Invoke(new Action(() => {' in method,
 'UI prompts stay on dispatcher':'dialog.Dispatcher.Invoke(new Action(() => {' in method and 'api.Dialogs.ShowMessage(' in method,
 'cancel before recording':'cancellation.Token.ThrowIfCancellationRequested();\n                            review.Confirm(' in method,
 'cancel between exact records':'if (cancellation.IsCancellationRequested) {canceled=true;break;}' in method,
 'stops on user no':'if (declined) break;' in method,
 'fail closed on errors':'catch (Exception ex) {failure=ex.Message;}' in method,
 'no auto preflight before real check':'nativeBatchButton.IsEnabled=false;' in method and 'checkButton.IsEnabled=true;' in method,
 'same known shared writer unchanged':shared.count('CatalogEnrichmentWriter.ApplyOne(')==1 and single.count('WorkspaceImportCoordinator.ApplyOne(')==1,
 'native batch remains confirmed-only': 'WorkspaceNativeSingleImport.Execute(' in batch and
   'entries.Select(x=>x.GameId).Distinct().Count()!=entries.Count' in batch,
 'no direct Playnite DB writes in UI': 'api.Database.Games.Update(' not in ui,
}
for label,ok in checks.items():
 if not ok: raise AssertionError('FAIL v0.5.48 reviewed identities: '+label)
print('PASS:',len(checks),'native in-review identity safety assertions; Windows/Playnite runtime unverified')
