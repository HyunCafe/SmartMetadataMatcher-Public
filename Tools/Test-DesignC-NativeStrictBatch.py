#!/usr/bin/env python3
"""v0.5.52 static invariants: strict verifier + original importer; never import on cached status."""
from pathlib import Path
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
single=(r/'Source/WorkspaceNativeSingleImport.cs').read_text(encoding='utf-8-sig')
batch=(r/'Source/WorkspaceNativeConfirmedBatch.cs').read_text(encoding='utf-8-sig')
strict=(r/'Source/WorkspaceFreshStrictLookup.cs').read_text(encoding='utf-8-sig')
contract=(r/'Source/WorkspaceBatchReview.cs').read_text(encoding='utf-8-sig')
checks={
 'strict proof read from live current game only':'FreshStrictReady(selected,current)' in ui,
 'no cached score as proof':'StrictEvidence=FreshStrictReady(x,input)' in ui and 'StrictEvidence = x.StrictEvidence' in contract,
 'batch still requires scoped exact field checks':'WorkspaceBatchLivePreflight.DetailedPlanReady(plan,verifiedTargets,verifiedDetails)' in ui,
 'batch requires confirmed OR strict-ready':'chosen.All(x=>NativeBatchReady(x))' in ui and 'row.IdentityConfirmed || FreshStrictReady(row,ScanInput(game))' in ui,
 'strict evidence frozen at approval':'FreshStrict=strict' in ui and 'strict = !selected.IdentityConfirmed && FreshStrictReady(selected,current)' in ui,
 'strict evidence rechecked after final dialog':'entry.FreshStrict' in ui and '!FreshStrictReady(selected,current)' in ui,
 'confirmed fallback needs persistent ID': 'againConfirmed.TryMatch(current.Id,current.Name,current.Platform,' in ui,
 'strict route explicit':'WorkspaceNativeSingleImport.ExecuteStrict(' in batch,
 'confirmed route preserved':'WorkspaceNativeSingleImport.Execute(' in batch,
 'sequential stop-on-first-failure':'result.Failed=1;' in batch and 'break;' in batch,
 'fresh full resolver immediately before download':'CatalogCore.QueryVariants(approvedIdentity.Name,approvedIdentity.Platform)' in single and
     'CatalogWindow.ResolveWithExactFallback(approvedIdentity,matches,client,token)' in single and
     single.index('CatalogCore.CanAutoPlan(resolved)')<single.index('exact = client.FetchById(catalogId,token);'),
 'full strict typed gate':'CatalogCore.CanAutoPlan(resolved)' in single and
     'resolved.Game.Id!=catalogId' in single and
     'String.Equals(resolved.Game.Name,catalogTitle,StringComparison.Ordinal)' in single,
 'live provider ID/platform after strict':'exact.PlatformIds.Contains(CatalogCore.PlatformId(approved.Platform))' in single,
 'final approval rechecks current source':single.count('review.EnsureOneCurrent(gameId,current,catalogId,catalogTitle,approvedTitle,draft);')==2,
 'strict Playnite metadata drift blocked':'Strict-matched Playnite identity changed before commit.' in single,
 'confirmed must load exact confirmation twice':single.count('if (!requireFreshStrict) RequireConfirmed(dataPath,current,catalogId,catalogTitle);')==2,
 'only original coordinator writes':single.count('WorkspaceImportCoordinator.ApplyOne(')==1 and 'WorkspaceImportCoordinator.ApplyOne(' not in batch and 'WorkspaceImportCoordinator.ApplyOne(' not in ui,
 'only existing journal':single.count('new EnrichmentJournal(dataPath)')==1 and 'new EnrichmentJournal(dataPath)' not in batch,
 'write never permitted by scan or simple marking':'WorkspaceNativeConfirmedBatch.Execute(' not in strict and 'new IgdbReadOnly(' not in ui,
 'one backup acknowledgement':'I verified a restorable backup of my Playnite library AND extension data.' in ui and 'if (confirm.ShowDialog()!=true) return;' in ui,
 'partial writes disclosed':'NOT an atomic all-or-nothing batch' in ui,
 'no ROM/file writes':all(t not in single+batch for t in ('File.WriteAllBytes(', 'Directory.Delete(', 'api.Database.Games.Update(')),
}
for name,ok in checks.items():
 if not ok:raise AssertionError('FAIL strict batch: '+name)
print('PASS:',len(checks),'fresh strict batch import safety assertions; Windows/Playnite runtime unverified')
