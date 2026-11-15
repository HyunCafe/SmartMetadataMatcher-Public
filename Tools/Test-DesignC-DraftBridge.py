#!/usr/bin/env python3
"""v0.5.38 source contracts: safe native draft transfer through the existing writer."""
from pathlib import Path
import re
r=Path(__file__).resolve().parent.parent
ui=(r/'Source/LibraryWorkspacePreview.cs').read_text(encoding='utf-8-sig')
classic=(r/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
plan=(r/'Source/WorkspaceBatchReview.cs').read_text(encoding='utf-8-sig')
tests=(r/'Tests/WorkspaceBatchReviewTests.cs').read_text(encoding='utf-8-sig')
writer=(r/'Source/CatalogEnrichmentWriter.cs').read_text(encoding='utf-8-sig')
manifest=(r/'extension.yaml').read_text(encoding='utf-8-sig')
plugin=(r/'Source/Plugin.cs').read_text(encoding='utf-8-sig')
checks=0
def check(pred,label):
    global checks
    checks+=1
    if not pred:raise AssertionError('FAIL v0.5.38 draft bridge: '+label)
for anchor in (
    'bool hadDraft = draftStates.ContainsKey(item.GameId)',
    'if (hadDraft && saved == null)',
    'saved.Matches(ScanInput(live), igdbId, item.CachedCatalogName)',
    'transferDrafts.Add(saved.Draft.Copy());',
    'classic.StageTransferredDrafts(transferDrafts);',
    'classic.StageVerifiedNativeReview(verifiedReview);',
    'No data is imported until you explicitly approve the final plan',
):check(anchor in ui,'native transfer '+anchor)
for anchor in (
    'private readonly HashSet<Guid> requiredTransferredDrafts',
    'internal void StageTransferredDrafts(IEnumerable<CatalogImportDraft> incoming)',
    'snapshot.Count > WorkspaceReviewHandoff.MaximumSelectedGames',
    '!CatalogImportDraft.Valid(d)',
    'requiredTransferredDrafts.Contains(item.Row.GameId) && currentDraft == null',
    'item.Draft=currentDraft==null?null:currentDraft.Copy();',
    'if (!item.RenameRequested && item.Draft!=null && item.Draft.CustomTitleEnabled)',
    'item.RenameRequested=rename &&',
    'automatic && item.Row.SmartAutoEvidence && !item.Row.UserConfirmed',
    'PlanBatchTitles(items,rename,automatic)',
    'item.RenameRequested,item.Extra,',
    'requiredTransferredDrafts.Remove(row.GameId);dialog.DialogResult=true;',
    'WorkspaceImportCoordinator.ApplyOne(api,enrichmentJournal,',
    'if(!JournalAllowsNewWrites()) return;',
):check(anchor in classic,'Classic reviewer '+anchor)
for forbidden in (
    'Uncheck Use catalog title to continue',
    'useCatalogTitle.IsChecked=false;',
    'actions.Children.Add(useCatalogTitle);',
):check(forbidden not in classic,'legacy toggle is gone: '+forbidden)
for anchor in (
    'StrictEvidence = x.StrictEvidence',
    'plannedNames[index] = Tuple.Create(current.GameId,title,current.Platform);',
    'Staged draft — transferred for Classic final review',
):check(anchor in plan,'pure title/evidence plan '+anchor)
for anchor in (
    'same-batch duplicate display titles blocked',
    'second proposed title is reserved with a safe platform suffix',
):check(anchor in tests,'regression test '+anchor)
check('internal static CatalogApplyResult ApplyOne' in writer,
      'established writer exists')
check('journal.MediaStaging.Begin(e.OperationId,e.GameId)' in writer,
      'pre-write media staging preserved')
check('journal.Prepare(e)' in writer and 'journal.Commit(e.OperationId)' in writer,
      'original journal lifecycle preserved')
# This regression test was introduced with v0.5.38, but it also runs in
# later releases. Require synchronized versions instead of pinning v0.5.38.
versions = re.findall(r'(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$', manifest)
check(len(versions) == 1, 'exactly one valid manifest version')
version = versions[0]
check(tuple(int(part) for part in version.split('.')) >= (0, 5, 38),
      'draft bridge regression applies to v0.5.38 and newer')
check('AssemblyVersion("' + version + '.0")' in plugin,
      'DLL version must match manifest')
check('AssemblyFileVersion("' + version + '.0")' in plugin,
      'file version must match manifest')
# The native library and history panels opened EnrichmentJournal readers in
# v0.5.37 already. Constructing a journal does NOT create a writer. Those
# existing usages call Committed()/AllEntries()/recovery inspection only.
# Guard their exact count to detect new consumers, but forbid mutation and
# actual Playnite database writes instead of banning valid read-only history.
check(ui.count('new EnrichmentJournal(') == 2,
      'native UI has exactly the two established read-only journal consumers')
for anchor in ('var completed = journal.Committed()',
               'var history = journal.AllEntries()',
               'journal.MediaStaging.AllStages()',
               'CatalogRecoveryConsistency.Inspect('):
    check(anchor in ui, 'read-only journal/recovery inspection: '+anchor)
for forbidden in (
    'Database.Games.Update(', 'CatalogEnrichmentWriter.ApplyOne(',
    'CatalogMetadataWriter.ApplyOne(', 'new CatalogMediaStagingJournal(',
    'journal.Prepare(', 'journal.Commit(', 'journal.MarkUndone(',
    'journal.MediaStaging.Begin(', 'journal.MediaStaging.Track(',
    'journal.MediaStaging.HandOff(', 'journal.MediaStaging.AbandonClean('):
    check(forbidden not in ui and forbidden not in plan,
          'native UI/preview must not perform imports or journal mutation: '+forbidden)
print('PASS:',checks,'guarded draft transfer, title safety, batch reservation and writer contract checks')
