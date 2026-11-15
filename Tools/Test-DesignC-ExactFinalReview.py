#!/usr/bin/env python3
"""Static smoke checks for the v0.5.43 native-to-Classic exact final review bridge."""
from pathlib import Path
root=Path(__file__).resolve().parent.parent
contract=(root/'Source/WorkspaceReviewContract.cs').read_text(encoding='utf-8-sig')
classic=(root/'Source/CatalogWindow.cs').read_text(encoding='utf-8-sig')
tests=(root/'Tests/WorkspaceBatchReviewTests.cs').read_text(encoding='utf-8-sig')
expected=[
 (contract,'internal void EnsureSamePlan(','no shared same-plan guard'),
 (contract,'WorkspaceBatchLivePreflight.SameDraft(snapshot.Draft, item.Item5)','missing draft equality'),
 (contract,'snapshot.IgdbId != item.Item2','missing IGDB identity equality'),
 (contract,'snapshot.PlannedTitle, item.Item4','missing proposed-title equality'),
 (contract,'proposed.Count != entries.Count','missing selected-scope equality'),
 (contract,'internal string DescribeVerifiedFields(Guid gameId)','missing bounded field snapshot'),
 (classic,'stagedNativeReview.EnsureSamePlan(','Classic must verify native plan'),
 (classic,'stagedNativeReview.DescribeVerifiedFields(x.Row.GameId)','Classic must show native field proposal'),
 (classic,'stagedNativeReview.EnsureCurrent(id =>','Classic must recheck Playnite snapshot'),
 (tests,'dropping a reviewed game cannot authorize Classic approval','missing scope negative'),
 (tests,'changed IGDB ID cannot authorize Classic approval','missing ID negative'),
 (tests,'changed final title cannot silently replace native plan','missing title negative'),
 (tests,'new staged draft cannot silently replace native field choices','missing draft negative')]
for source, phrase, label in expected:
 if phrase not in source:raise AssertionError('FAIL v0.5.43: '+label)
for forbidden in ('CatalogEnrichmentWriter.ApplyOne(', 'Database.Games.Update(',
                  'journal.Commit(', 'journal.Prepare('):
 if forbidden in contract:raise AssertionError('FAIL independent writer in review contract: '+forbidden)
print('PASS:',len(expected),'v0.5.43 exact review and single-writer source checks')
