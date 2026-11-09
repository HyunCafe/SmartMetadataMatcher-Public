# Title-independent matching audit

Source candidate, November 9, 2026. This is not a released `.pext` or a claim of universally correct game identification.

> Historical audit snapshot. Both matching-fix branches were subsequently built and tested on Windows by the maintainer, then consolidated into public `main`. The access and execution limits below describe the original audit environment. See [current readiness](../RELEASE_READINESS.md) for release status.

## Scope and evidence

The public baseline is commit `3825bfc63ee4019d1a03507f84d9110572e79e01`. The audit inventories all 34 production C# files, examines matching and its discovery, UI, preflight, display-title and recovery callers, and searches production code for named-game branches, fixed game IDs and personal paths. The archived v0.5.17 source snapshot has the same 64 source, test and tooling files as this baseline after newline normalization. The current private repository could not be accessed; its live state has **not** been verified or changed.

The corrections are intended for both checkouts. The installer applies only to compatible source and runs the full Windows test project and plugin build before committing. It does not replace either working tree or release a binary.

## Findings and changes

| Area | Previous assumption | Revised behavior |
| --- | --- | --- |
| Query normalization | Named-game rewrite dictionaries and franchise exceptions | No game-title lookup tables or franchise allowlists in production matching. Catalog candidates supply identities. |
| Query expansion | Abbreviation and installment discovery favored a handful of handhelds | Discovery-only abbreviation, list-prefix and ordinal/subtitle searches use the same rules on any platform. Query budget remains bounded. |
| List indices | Two digits could be discarded as identity evidence on selected consoles | Preserve the original number for identity. Search a stripped variant as an untrusted lead on any platform. Even a consecutive collection is a rename suggestion requiring review. |
| Platform tags | PSP suffix removal was implicitly trusted | Matching platform tags are discovery/review hints on every mapped platform. They are not proof that a word is absent from the official title. |
| Series structure | A local `1` or `2` after two words was assumed to be an informal installment label | Supplied ordinals cannot be silently discarded. Missing **catalog** ordinals may be inferred from a distinctive intact subtitle after series names of different lengths, with rival/platform checks. |
| Episode wrappers | A fixed two-word series prefix | Match a shared prefix of variable length, an explicit ordinal wrapper and an intact distinctive subtitle. |
| Rival ranking | Popularity could favor the familiar game | Popularity alone cannot resolve ownership or identity. Competing plausible releases require independent evidence. |
| Automatic selection | Fast exact/alias path could avoid expanded resolver release-type checks | Both automatic paths pass the same live title/platform/release compatibility gate. Missing type or DLC cannot qualify merely through an exact-looking title. |
| Sequels and editions | Prefixes/typos could omit additional numerals; `+` was punctuation | Bind numeric and alphanumeric identity tokens; preserve `+` count in lookup, matching and proposed display names. Protect edition/version markers. |
| Platform verification | Substring matches could turn combined/custom platform names into a supported console | Exact documented aliases; unknown and multi-platform strings stay unverified. Add missing aliases for already mapped platforms. |
| Short titles | One- and two-character names rejected before querying or proposing display titles | Accept nonempty bounded titles; existing type/platform/ambiguity checks still apply. |

A list prefix and a legitimate numeric title can look identical. Removing that distinction to retain every previous automatic match would recreate the overfitting problem. Such entries remain discoverable and can use the existing explicitly reviewed batch workflow.

## What remains automatic

Unique compatible full titles, supported Roman numeral forms, sufficiently corroborated aliases, distinctive subtitle matches, eligible omissions of catalog ordinals, and appropriate same-title release comparisons still use the evidence-based resolver. Fresh qualifying games can enter one approved batch. Name changes remain optional. Live ID/platform checks, missing-field defaults, conflict detection, stop-on-failure, journals, and conditional undo remain in force.

The older fixture names remain useful regressions. They are **test data**, not production exceptions. Clean-title fixtures now isolate identity behavior, while a separate matrix explicitly checks that the formerly stripped numbers and platform tags do not confer automatic approval.

## Validation

`Tests/UniversalMatchingTests.cs` adds 1,882 deterministic assertions across five unfamiliar series names, varied series lengths, all ten mapped platforms, alternate record IDs and order, popularity changes, same-title rivals, wrong platforms, numeral conflicts, editions, local annotations, explicit episode wrappers, short names and three process cultures. The full existing offline test runner also passes, including mocked connection errors, cache/confirmation invalidation, selection policies, field protection and recovery fault fixtures.

Executed here using an isolated .NET 8 Linux harness compiling the repository's listed test sources with C# 5. This is **not** a Windows `net462`/WPF build or a Playnite integration run. The public source inventory/privacy check and `git diff --check` are also required. No live game library was modified. No controlled live IGDB accuracy/recall benchmark was performed.

## Remaining bounds

- Structured platform verification still supports ten mapped platform IDs. Unknown/custom platforms and combined platform strings are not guessed. A provider-backed platform registry and explicit multi-platform selection need separate implementation and validation.
- Title vocabulary and some normalization remain language-dependent; the fixtures do not establish coverage for all languages or Unicode writing systems. Unlabelled single Roman letters and translated titles can require review.
- Search is bounded, not exhaustive catalog enumeration. A completed exact/prefix lookup is not proof that no hidden alias or omitted provider record exists.
- Heuristic scores, token counts and subtitle rules are not calibrated accuracy probabilities. Similar names can still need human judgment.
- Runtime UI, dependency provenance, large batches and interruption behavior still require the Windows/Playnite acceptance gates in `RELEASE_GATE.md`.

## Production inventory reviewed for personalization

| Files | Role and result |
| --- | --- |
| `CatalogCore`, `CatalogSmartResolver`, `NameRules`, `EnrichmentPlan` | Shared matching/discovery/title decisions; changed as above. |
| `CatalogWindow`, `PreviewWindow`, `CatalogQueueView` | UI entry, scan, review and live recheck callers. No named-game exemptions found; existing UI routes consume the revised pure decisions. |
| `IgdbReadOnly`, `CatalogCandidateEvidence` | Bounded provider requests and diagnostic evidence. No fixed game IDs; preserve credentials, cancellation and rate pacing. |
| `CatalogReviewActions`, `CatalogImportDraft`, `CatalogImportIntent`, `CatalogEnrichmentNeeds` | Per-game approval context, proposed fields and field availability. No personal-library exceptions found. |
| `EnrichmentJobPlan`, `CatalogBatchPolicy`, `CatalogBatchProgress`, `CatalogManualReviewFlow`, `CatalogStrictBatchFlow` | Selection, counts and approval flow. Generic resource limits and evidence checks retained. |
| `CatalogMetadataWriter`, `CatalogEnrichmentWriter`, `MetadataPlan` | Verify reviewed identities and expected current fields; fill missing values and preserve launch data. No title-specific write privileges found. |
| `CatalogScanCache`, `CatalogConfirmedIdentities`, `CatalogIgnored` | Per-user cache and explicit decisions remain local data, not bundled global matching rules. |
| `MetadataJournal`, `EnrichmentJournal`, `RenameJournal`, `CatalogMediaStagingJournal`, `CatalogRecoveryConsistency` | Operation/game IDs and before/after values govern recovery. No game-title exceptions found. |
| `DiagnosticCsv`, `DiagnosticExport`, `HistoryViewFilter`, `HistoryVisibilityStore`, `Plugin` | Diagnostics, presentation and plugin registration. Author attribution and the stable extension GUID are product identity, not personal matching logic. |
| Project files, manifests and `Tools/*` | Dependency pins, packaging and release checks. Retained release/version identity; no runtime game matching rules. |
