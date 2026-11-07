#!/usr/bin/env python3
"""Read-only source consistency check for Smart Metadata Matcher.

No network access, Git writes, Playnite access, tokens, or file mutations.
"""
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

root = Path(__file__).resolve().parent.parent
problems = []
manifest = (root / "extension.yaml").read_text(encoding="utf-8-sig")
plugin = (root / "Source/Plugin.cs").read_text(encoding="utf-8-sig")
yaml_version = re.search(r"(?m)^Version:\s*(\d+\.\d+\.\d+)\s*$", manifest)
assembly = re.search(r'AssemblyVersion\("([0-9.]+)"\)', plugin)
file_version = re.search(r'AssemblyFileVersion\("([0-9.]+)"\)', plugin)
if not yaml_version or not assembly or not file_version:
    problems.append("Missing manifest/assembly version declaration")
elif assembly.group(1) != yaml_version.group(1)+".0" or file_version.group(1) != assembly.group(1):
    problems.append("Manifest, assembly and file versions differ")

for file, kind in (("CatalogWindow.cs", "CatalogWindow"), ("PreviewWindow.cs", "PreviewWindow")):
    content = (root / "Source" / file).read_text(encoding="utf-8-sig")
    if f"typeof({kind}).Assembly.GetName().Version" not in content:
        problems.append(f"{file}: window must use runtime assembly version")

for project, folder, expected_set in (
    ("SmartMetadataMatcher.csproj", "Source", set(p.as_posix() for p in (root/"Source").glob("*.cs"))),
    ("Tests/NameRulesTests.csproj", "Tests", set(p.as_posix() for p in (root/"Tests").glob("*Tests.cs")))
):
    try:
        tree = ET.parse(root / project)
        includes = [(root / Path(project).parent / elem.attrib["Include"]).resolve()
                    for elem in tree.iter("Compile") if "Include" in elem.attrib]
        missing = [path for path in includes if not path.is_file()]
        if missing:
            problems.append(f"{project}: references missing source files")
        relative = {p.as_posix() for p in includes if p.parent.name == folder}
        if relative != expected_set:
            problems.append(f"{project}: compile inventory differs from {folder} folder")
    except (ET.ParseError, OSError) as ex:
        problems.append(f"{project}: {type(ex).__name__}")

program = (root / "Tests/NameRulesTests.cs").read_text(encoding="utf-8-sig")
for path in (root/"Tests").glob("*Tests.cs"):
    if path.name == "NameRulesTests.cs":
        continue
    # Every suite should be invoked by the main offline test runner.
    stem = path.stem
    if stem + ".Run();" not in program:
        problems.append(f"Test suite not invoked: {stem}")

# RC3 precision + CSV audit invariants: no low-confidence mislabeling,
# consistent edition gate, and auditable missing-field export.
core=(root/"Source/CatalogCore.cs").read_text(encoding="utf-8-sig")
view=(root/"Source/CatalogWindow.cs").read_text(encoding="utf-8-sig")
needs=(root/"Source/CatalogEnrichmentNeeds.cs").read_text(encoding="utf-8-sig")
if not all(s in core for s in ("HasMaterialEditionDifference", "match.EditionConflict",
    "editionConflict=HasMaterialEditionDifference", "first.Item2<0.55")):
    problems.append("RC3 edition/irrelevant-candidate gates missing")
if not all(s in view for s in ("Missing Playnite fields at export", "CandidateOverlap(mask,r.AvailableMetadata)",
    "live==null?r.MissingMask:MissingMask(live)")) or "internal static string Describe(int mask)" not in needs:
    problems.append("RC3 export missing live field gap/evidence columns")

# RC4: low-similarity results may be human-reviewed only after structured
# platform/canonical-title corroboration. Exact fallback is read-only and
# Series-only browsing must remain reversible without editing Playnite.
igdb=(root/"Source/IgdbReadOnly.cs").read_text(encoding="utf-8-sig")
if not all(s in core for s in ("PlausibleManualReviewAnchor", "BuildExactNameQueryBody",
    "manualAnchor", "expectedPlatformId>0", "first.Item2<0.55")):
    problems.append("RC4 review-only relevance, platform and exact-title gates missing")
if not all(s in igdb for s in ("SearchExactTitle(string title", "BuildExactNameQueryBody(title)",
    'String.Equals((x.Name??"").Trim(),title.Trim()')):
    problems.append("RC4 bounded exact-title IGDB fallback missing")
if not all(s in view for s in ("missingFilter", "VisibleInMissingScope", "Priority(x.MissingMask)",
    "client.SearchExactTitle(title,token)")):
    problems.append("RC4 optional Series scope and read-only fallback wiring missing")
if not all(s in needs for s in ("IsSeriesOnly(int mask)", "VisibleInMissingScope(int mask,int scope)",
    "Priority(int mask)")):
    problems.append("RC4 Series-only priority/visibility guards missing")

# Review navigation invariant: draft edits must preserve the current filter.
# IGDB identity adoption is the only context-menu operation that may reset it.
review_source = (root / "Source/CatalogWindow.cs").read_text(encoding="utf-8-sig")
checks = (
    "Window reviewOwner,Action<bool> refreshReview",
    "refreshReview(false);else UpdatePreview();",
    "refreshReview==null?null:new Action(()=>refreshReview(true))",
    "},dialog,identityChanged=>{",
    "if(!identityChanged)",
    "selected.RenameHandling=DraftTitlePreview(selected.SourceRow);",
)
if not all(fragment in review_source for fragment in checks):
    problems.append("Review context actions must distinguish draft edits from IGDB identity changes")
if not re.search(r"if\(!identityChanged\)\s*\{[^{}]*?refreshRows\(\);[^{}]*?return;\s*\}", review_source, re.S):
    problems.append("Draft edits must refresh only the current filtered review view")
# PlanPreviewRow is a lightweight wrapper. Batch policy must reflect the same
# confirmation state as the underlying CatalogViewRow, not a stale copy.
preview_class = review_source.split("internal sealed class PlanPreviewRow", 1)
if len(preview_class) != 2 or "public bool UserConfirmed" not in preview_class[1].split("internal sealed class HistoryRow", 1)[0]:
    problems.append("PlanPreviewRow must expose the underlying user-confirmed state")

# Batch-size and fail-fast policy must be used in both review and writer paths.
policy=(root / "Source/CatalogBatchPolicy.cs").read_text(encoding="utf-8-sig")
if "StrictLimit" in policy or not all(x in policy for x in (
    "SupervisedLimit = 5", "ReviewedBulkLimit = CatalogManualReviewFlow.MaxReviewedBatch",
    "if(strictBatch && reviewedBulk)", "!row.UserConfirmed || row.FromSavedScan || row.FromHistory")):
    problems.append("Strict/five-supervised/fifty-reviewed batch policy does not preserve safety gates")
if "CatalogBatchPolicy.WithinLimit(selected.Count,strictBatch,reviewedBulk)" not in review_source or \
   "CatalogBatchPolicy.ValidateSelection(marked.Select" not in review_source:
    problems.append("Review and writer batch-size gates must agree")
if review_source.count("CatalogBatchPolicy.AvailableSelectionSlots(")<2:
    problems.append("Strict-ready selection and UI display must use shared policy")
if "stoppedOnError=CatalogBatchPolicy.StopOnFailure(true);" not in review_source:
    problems.append("The import loop must stop after its first failure")

if "CatalogBatchPolicy.ValidateSelection(marked.Select" not in review_source:
    problems.append("Every import batch must pass explicit reviewed-selection validation")
if not (root / "Tools/Verify-Package.py").is_file():
    problems.append("Missing compiled-package integrity verifier")
if "Tools/Verify-Package.py" not in (root / "Tools/Build-Preview.ps1").read_text(encoding="utf-8-sig"):
    problems.append("Package verifier must run before reporting build success")


# v0.5.4: UI status must distinguish cached evidence from verified, and
# selection text must accurately describe selecting all (not just the next N).
if not all(x in view for x in ('Column("Next step","DisplayStatus",170)',
    'Select all ready', 'Select visible candidates',
    'Saved match — refresh IGDB before strict import')):
    problems.append("v0.5.4 readiness and bulk-selection UI wording missing")
if 'DashboardStatus(' not in (root/'Source/EnrichmentJobPlan.cs').read_text(encoding='utf-8-sig'):
    problems.append("v0.5.4 dashboard readiness policy missing")

# v0.5.5: offline test runner must compile the same HTTP client as production,
# and credentials are not automatically saved when a connection is tested.
conn=(root/'Source/IgdbReadOnly.cs').read_text(encoding='utf-8-sig')
if not all(k in conn for k in ('TestConnection(CancellationToken ct)',
    '"fields id; limit 1;"', 'IgdbAuthenticationException',
    'IgdbConnectionResult.FromHttp', 'if(ct.IsCancellationRequested)throw;')):
    problems.append('IGDB test must check both auth and bounded game API query')
if 'IgdbConnectionTests.Run();' not in program or 'Link="IgdbReadOnly.cs"' not in (root/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig'):
    problems.append('IGDB connection test seams must be compiled and invoked')
# v0.5.6 scan continuation: bounded on-worker checkpoints; final short-tail
# flush must occur after success, cancellation, and IGDB errors but before UI update.
cache=(root/"Source/CatalogScanCache.cs").read_text(encoding="utf-8-sig")
if not all(x in cache for x in ("internal sealed class CatalogScanCheckpoint",
    "internal const int DefaultInterval = 10", "cache.SaveBatch(pending,utcNow);",
    "pending.Clear();")):
    problems.append("Incremental atomic scan checkpoint buffering missing")
if not all(x in view for x in ("checkpoints.Add(SaveableScanResult(next[next.Count-1]),DateTime.UtcNow)",
    "try {checkpoints.Flush(DateTime.UtcNow);}",
    "if(finalCacheWarning!=null)scanCacheAvailable=false;")):
    problems.append("Read-only long scan must checkpoint completed rows and final tail")
if "checkpoint.SavedCount==20 && checkpoint.PendingCount==5" not in (root/"Tests/CatalogScanCacheTests.cs").read_text(encoding="utf-8-sig"):
    problems.append("Incremental scan restart/flush regression missing")

# v0.5.7 history controls are presentation-only and share a pure filter.
if not all(x in view for x in ("HistoryViewFilter.Includes(",
    "historyStateFilter.SelectionChanged+=(s,e)=>RefreshHistory();",
    "search.TextChanged+=(s,e)=>refreshFull();",
    'ElementStyle=HistoryTextStyle("Game")',
    'ElementStyle=HistoryTextStyle("Fields")',
    'selectedDetail.Text=selected.Count==1?')):
    problems.append("History search, status filtering, and full text details not wired")
if 'HistoryViewFilterTests.Run();' not in program or 'Link="HistoryViewFilter.cs"' not in (root/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig'):
    problems.append("History presentation policy must compile and run in offline tests")

journal=(root/'Source/EnrichmentJournal.cs').read_text(encoding='utf-8-sig')
if not all(x in journal for x in ('var byId=new Dictionary<Guid,EnrichmentEntry>();',
    'byId.ContainsKey(e.OperationId)', 'byId.Add(e.OperationId,e)',
    'byId.TryGetValue(id,out e) || e.Undone')):
    problems.append('Indexed journal replay must retain duplicate and undone validation')
if 'EnrichmentJournalReplayTests.Run();' not in program:
    problems.append('Indexed journal corruption/ordering tests not wired')

# v0.5.10: primary strict-ready action must bypass only intermediate marking,
# never the existing verified batch or backup-gated final confirmation.
if not all(x in view for x in (
    'AddButton(filters,"Apply verified matches...",AutoPlan,out verifiedBatchButton)',
    'AddButton(filters,"Review matches",ShowPlanReview,out reviewSelectionButton)',
    'var ready=CatalogStrictBatchFlow.EligibleIds(candidates,plan);',
    'RunBatch(safe,true,excluded);',
    'table.SelectionMode=DataGridSelectionMode.Extended;',
    'table.SelectionUnit=DataGridSelectionUnit.FullRow;',
    'table.SelectedItems.OfType<PlanPreviewRow>()',
    'AddButton(selectionTools,"Mark highlighted"',
    'AddButton(selectionTools,"Unmark highlighted"',
    'I verified a restorable backup of my Playnite library AND extension data.')):
    problems.append('Primary guarded verified-batch path or extended review selection missing')
if 'CatalogStrictBatchFlowTests.Run();' not in program or \
    'Link="CatalogStrictBatchFlow.cs"' not in (root/'Tests/NameRulesTests.csproj').read_text(encoding='utf-8-sig'):
    problems.append('Verified-batch routing tests not linked into offline runner')

# v0.5.11: explicit user-review path is bounded, atomic and gated before writes.
if not all(x in view for x in (
    'ConfirmSelectedIdentities(selected,dialog)', 'confirmedIdentities.ConfirmMany(records);',
    'CatalogManualReviewFlow.Validate(inputs)',
    'I personally verified EVERY listed game, platform and edition',
    'var verifiedRecords=new Dictionary<long,CatalogGame>();',
    'CatalogReviewActions.PlatformVerified(item.Platform,',
    'if(!cancelled && !stoppedOnError)',
    'RunBatch(', 'reviewedRows,strictBatch,excludedCount,reviewedBulk')):
    problems.append('Manual identity confirmation or upfront platform preflight missing')
if not all(x in program for x in ('CatalogManualReviewFlowTests.Run();',
    'CatalogConfirmedIdentitiesTests.Run();', 'CatalogBatchPolicyTests.Run();')):
    problems.append('Reviewed identity / batch policy offline tests not linked')
if problems:
    for problem in problems:
        print("STOP:", problem)
    sys.exit(1)
print("PASS: source versions, dynamic UI titles, compile inventories and test runner wiring")
print("READ-ONLY audit; does not certify binary security or Playnite runtime behavior")
