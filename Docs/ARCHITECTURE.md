# Architecture and safety boundaries

This document maps the **current source implementation** of Smart Metadata Matcher. The published [`v0.5.17-beta.1`](https://github.com/HyunCafe/SmartMetadataMatcher-Public/releases/tag/v0.5.17-beta.1) package predates the title-independent matching revisions on `main`. This is an implementation guide, **not** a runtime certification or a claim of measured matching accuracy.

## Execution model

Smart Metadata Matcher is a Playnite 10 .NET Framework 4.6.2 `GenericPlugin`. It executes **inside Playnite**, with the permissions of its Windows user; it is not sandboxed. The extension does not run an independent background service.

```text
Playnite menu / extension entry point
  Source/Plugin.cs
      |
      v
Dashboard, scan/review and import coordination
  Source/CatalogWindow.cs
      |
      +-- Read-only discovery and identity evidence
      |     IgdbReadOnly -> CatalogCore -> CatalogSmartResolver
      |     CatalogCandidateEvidence / CatalogScanCache
      |
      +-- Selection and approval
      |     CatalogStrictBatchFlow / CatalogBatchPolicy
      |     CatalogManualReviewFlow / CatalogConfirmedIdentities
      |     CatalogImportDraft / EnrichmentJobPlan
      |
      +-- Approved metadata writes and conditional undo
            CatalogEnrichmentWriter / EnrichmentPlan
            EnrichmentJournal / CatalogMediaStagingJournal
            CatalogRecoveryConsistency
```

The diagram groups responsibilities; it does not imply that each component directly calls the next or that the whole batch is one transaction.

## Data flow and trust boundaries

| Stage | Principal code | Boundary and behavior |
| --- | --- | --- |
| Read local game state | [`CatalogWindow`](../Source/CatalogWindow.cs), [`CatalogQueueView`](../Source/CatalogQueueView.cs) | Reads Playnite title, platform and metadata through the SDK. Scanning does not authorize a write or modify ROM files or launch actions. |
| Query remote catalog | [`IgdbReadOnly`](../Source/IgdbReadOnly.cs) | Requests Twitch OAuth and IGDB search/identity data. Game lookup terms and network information leave the machine; a successful lookup is not identity proof. |
| Evaluate candidates | [`CatalogCore`](../Source/CatalogCore.cs), [`CatalogSmartResolver`](../Source/CatalogSmartResolver.cs) | Compares title, platform, ordinals, editions, release data and plausible rivals. Similarity and ranking points are heuristics, not calibrated correctness probabilities. Search results are bounded and may be incomplete. |
| Prepare a reviewed selection | [`CatalogBatchPolicy`](../Source/CatalogBatchPolicy.cs), [`CatalogManualReviewFlow`](../Source/CatalogManualReviewFlow.cs), [`CatalogStrictBatchFlow`](../Source/CatalogStrictBatchFlow.cs) | Separates fresh strict eligibility from user-reviewed identities and weak-evidence supervision. Explicit review is not replaced by a high score. |
| Approve and recheck | [`CatalogWindow`](../Source/CatalogWindow.cs), [`CatalogReviewActions`](../Source/CatalogReviewActions.cs) | Shows proposed changes and requires approval and backup acknowledgement. Reviewed bulk selection validates all chosen IGDB identities live **before its first write**; other import paths perform live checks before each write. |
| Apply and recover | [`CatalogEnrichmentWriter`](../Source/CatalogEnrichmentWriter.cs), [`EnrichmentJournal`](../Source/EnrichmentJournal.cs), [`CatalogMediaStagingJournal`](../Source/CatalogMediaStagingJournal.cs) | Writes approved Playnite metadata serially, records write-ahead intent and staged media, and stops on failure. Completed changes are not rolled back atomically as a batch. |

## Persistence and recovery

- **User data:** Scan cache, saved identity decisions, ignored entries, history and journals reside in the Playnite extension's local user-data area, not in the public `.pext`. See [privacy](../PRIVACY.md).
- **Credentials:** The user supplies Twitch developer credentials; the source stores them locally with Windows CurrentUser DPAPI, while the OAuth token remains in memory. Code running under the same Windows user remains inside the trust boundary.
- **Field protection:** The writer compares live game state with the approved snapshot. Existing metadata values are preserved by default; deliberate edited descriptions or display-title changes require their respective approval flow. Shared Playnite genre/company/series entries are not automatically deleted by undo.
- **Crash and undo:** Per-operation field and media-staging journals support conditional undo and recovery checks. An unresolved or inconsistent journal blocks new writes rather than guessing a cleanup. These logs are **not a cross-file atomic database transaction**, and successful offline fault fixtures do not prove recovery after an actual Playnite process crash.
- **Scope:** ROM contents, emulator configuration, executable paths and launch actions are outside the writer's intended mutation scope.

## Navigation for reviewers and contributors

| Purpose | Start here |
| --- | --- |
| Plugin entry point and user workflow | [`Source/Plugin.cs`](../Source/Plugin.cs), [`Source/CatalogWindow.cs`](../Source/CatalogWindow.cs) |
| Title and platform matching | [`Source/CatalogCore.cs`](../Source/CatalogCore.cs), [`Source/CatalogSmartResolver.cs`](../Source/CatalogSmartResolver.cs), [`Source/NameRules.cs`](../Source/NameRules.cs) |
| Batch approval and identity persistence | [`Source/CatalogBatchPolicy.cs`](../Source/CatalogBatchPolicy.cs), [`Source/CatalogConfirmedIdentities.cs`](../Source/CatalogConfirmedIdentities.cs) |
| Metadata writing, history and recovery | [`Source/CatalogEnrichmentWriter.cs`](../Source/CatalogEnrichmentWriter.cs), [`Source/EnrichmentJournal.cs`](../Source/EnrichmentJournal.cs), [`Source/CatalogRecoveryConsistency.cs`](../Source/CatalogRecoveryConsistency.cs) |
| Offline tests | [`Tests/NameRulesTests.cs`](../Tests/NameRulesTests.cs), [`Tests/UniversalMatchingTests.cs`](../Tests/UniversalMatchingTests.cs), [`Tests/JournalFaultMatrixTests.cs`](../Tests/JournalFaultMatrixTests.cs) |
| Reproducible Windows candidate build | [`Tools/Build-Candidate.ps1`](../Tools/Build-Candidate.ps1), [Windows CI](../.github/workflows/windows-ci.yml) |

The current supported build entry point compiles the Playnite extension, runs offline tests and checks a local candidate `.pext`. CI artifacts are **not approved releases**. Source checks, synthetic matching tests and disposable-file journal tests do not establish real-world matching accuracy, dependency trust or Playnite host behavior.

For the remaining verification gates see [release readiness](../RELEASE_READINESS.md), [release criteria](RELEASE_GATE.md) and the [evaluation plan](EVALUATION.md). For historical rationale see [design decisions](../DECISIONS.md); those decisions record past checkpoints, not current approval.
