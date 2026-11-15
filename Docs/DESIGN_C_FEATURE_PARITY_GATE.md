# Design C — Feature-Parity Gate for Smart Metadata Matcher

**Current interpretation (v0.5.67):** The 48 rows below are a **preservation and release-acceptance checklist**, originally drafted against v0.5.19, not an assertion that those features are currently missing. The current source checkout and verified test results take precedence over historical implementation notes. Design C already implements native scan/review, exact-ID field checks, custom drafts, individual and batch approval paths, History, newest-first undo, ignore/restore, and diagnostics **in source**. No source-level or offline test is evidence of successful real Playnite import, undo, or recovery. Classic stays as fallback until that acceptance is observed.

**Functional consolidation policy:** Scan → Review → Apply → Undo is one user-facing flow. Reuse the existing shared writer and journal. Consolidate duplicate entry points and review steps during the cohesive UI phase; retain essential advanced capabilities without replicating every Classic button. Never add an alternate metadata writer. **Automatically plan safe Playnite display-title cleanup on approved imports; there is no opt-out checkbox.** Show the exact proposed name before approval, preserve titles for unconfirmed smart-inferred identities, and retain collision, edition, custom-title, and launch-path safeguards. These source safeguards do not establish runtime parity.

**Review-flow consolidation (v0.5.64):** Focused review and marked-batch review keep separate exact GameId scopes. A one-game dialog shows a compact current/proposed title and identity summary; a batch retains its full table. The UI exposes only the applicable native Apply action, with Classic retained as a secondary fallback. Both actions still require the pre-existing live exact-field, approval, and backup gates; this is not a new importer or runtime certification.

**Navigation consolidation (v0.5.65):** The existing focused-game review is accessible through the inspector, right-click menu, double-clicking an eligible game row, or Ctrl+Enter. The old read-only details remain accessible by context menu and when double-clicking an ineligible game; double-clicking the Mark checkbox never launches a modal. Selection and import approval remain separate. This is navigation parity, not runtime write/undo certification.

**Review presentation (v0.5.66):** A single marked game and a focused game now share the same compact preview, without changing GameId scope. Batch overview columns omit repeated titles and per-field detail already available in the separately verified field-by-field plan; full cell values remain accessible through tooltips. The read-only review grid does not use click-to-select. Next-step instructions and visibility of identity/field-detail actions reflect actual review prerequisites; all existing identity, preflight, approval, backup, Classic, and journal gates remain intact. This is UI/source verification, not import/undo runtime evidence.

**Cohesive workspace pass (v0.5.67):** Make the everyday sequence obvious without removing advanced capability. The library mark tray prioritizes review, the game inspector presents review → live field check → optional draft in a clear panel, and less-frequent identity/ignore/evidence operations remain reachable via an expander and context menu. The existing review dialog separates prerequisites from final decision actions and limits one-game empty grid space. History preserves all conditional undo, hidden-entry and recovery functions, moving only presentation visibility commands into one menu. UI layout source checks do not prove WPF rendering; inspect spacing/scrolling in Playnite before advancing. No existing write, cache, identity or journal contract has changed.

**Historical scope:** The initial gate used the working v0.5.19 checkout rather than older public `main`. The earlier phase notes and version numbers below describe the origin of the plan, not the current progress state.

**Release rule:** A feature is not considered preserved because it appears in an HTML prototype. It must be wired into the real Playnite extension, tested against a real or controlled library, and accessible through the UI. Keep **Open classic Matcher** available until the new path passes the acceptance cases. The current plugin must not be replaced by a prototype.

Abbreviations: **Main** = ordinary library workspace; **Context** = game inspector, review, or confirmation; **Advanced** = tools/history/diagnostics; **Core** = unchanged business logic; **TBD** = not implemented in Design C yet.

| # | Existing function / requirement | Existing code / evidence | Destination | Preservation and acceptance gate |
|---|---|---|---|---|
| 01 | First-run Twitch/IGDB credentials | `CatalogWindow.ShowCredentialSettings`, `CatalogCredentialStore` | Main setup + Advanced | Show purpose and exact setup instructions; test connection, never display secret in UI |
| 02 | Twitch connection diagnostic | `IgdbReadOnly`, `Test Connection` | Context setup | Cancellable test, distinguishes auth/network/permission errors |
| 03 | Scan imported Playnite games | `CatalogWindow.ScanInput`, `StartScan` | Main: Check library | Never scans disk ROMs; explain Playnite import prerequisite |
| 04 | Existing/new game eligibility | `queue`, `ScanInput` | Core → Main | Newly imported game IDs enter pending list without forcing full rescan |
| 05 | Incremental scan cache | `CatalogScanCache`, locally built v0.5.18 | Core | 504 stored/0 pending survives reopen unchanged; counts unambiguous |
| 06 | Resume/checkpoint every 10 | `CatalogScanCheckpoint` | Core + progress UI | Cancellation/restart preserves all completed checkpoints, saves final tail |
| 07 | Scan batch size and cancellation | `scanSize`, `Stop after current game` | Main progress + Advanced | No silent 504-game network job; pause/cancel at safe boundaries |
| 08 | Selective refresh of saved results | `RefreshSelectedCatalogMatches` | Context preflight | Selected IDs only; no full-cache archival |
| 09 | Refresh-required count/action | local v0.5.19, user screenshots | Core → Context | 393 cached refresh can be targeted; do not require 393 refresh for 20 selected games |
| 10 | Explicit full rescan | `RescanAllCatalogMatches` | Advanced | Clear confirmation and saved-cache archive; does not modify user metadata |
| 11 | Titles and ROM prefix normalization | `NameRules`, `CatalogCore` | Core | Preserve ordinals, editions, consoles, and test regressions |
| 12 | IGDB search variants/exact/prefix | `ResolveWithExactFallback`, `IgdbReadOnly` | Core | Bounded lookup, platform evidence, no weak-evidence autoapproval |
| 13 | Rival and edition evidence | `CatalogSmartResolver`, `CatalogCandidateEvidence` | Core + Context details | Ambiguous editions stay for human review |
| 14 | Strict automatic candidate evidence | `CatalogCore.CanAutoPlan` | Core | Cached score never confers write permission |
| 15 | Persist explicit identity confirmation | `CatalogConfirmedIdentities` | Core + Context | Distinct from an applied import; invalidated when identity changes |
| 16 | Manually search IGDB alternatives | `ShowIgdbAlternativeSearch` | Context | Accessible from any uncertain/suspect game |
| 17 | Inspect exact IGDB record / optional fields | `InspectIgdbFields` | Context | Extra fields checked before declaring no useful updates |
| 18 | Side-by-side current/proposed values | `ShowFullComparison`, `previewComparison` | Main inspector | Clear field-level differences, expandable long descriptions |
| 19 | Manual title, description and field drafts | `CatalogImportDraft`, `EditImportDraft` | Context | Keep untouched until approved, drafts invalidate on identity change |
| 20 | Standard safe Playnite display-title cleanup | `PlanBatchTitles`, `CatalogImportDraft.ResolveTitle` | Context final plan | Always plan on eligible approved imports (no checkbox); preview exact name, preserve unconfirmed smart-inferred titles, reject collisions/unsafe editions, allow explicit custom title |
| 21 | Never rename ROM files or launch paths | `CatalogEnrichmentWriter` scope | Core | Verify no change to ROM files, executable paths, emulator config |
| 22 | Preserve existing populated fields | `MetadataPlan` / `CatalogEnrichmentWriter` | Core | Missing-field enrichment only by default; edited description explicit |
| 23 | Description, cover, icon, background | `CatalogEnrichmentNeeds`, writer | Main/Context + Core | Per-field choices and preview |
| 24 | Release, genres, developers, publishers, series | `CatalogEnrichmentNeeds`, writer | Main/Context + Core | Include optional fields only after inspection; don't silently discard |
| 25 | Strict selected batch preflight | `CatalogStrictBatchFlow`, `CatalogBatchPolicy` | Context final plan | Fresh exact-ID/platform check; select by exact Playnite GameId |
| 26 | Manual supervised and reviewed bulk caps | `CatalogManualReviewFlow`, `CatalogBatchPolicy` | Context | Preserve 5-supervised / up-to-1000-reviewed gates as appropriate |
| 27 | Mixed strict/manual selections | `ShowPlanReview`, `RunBatch` | Context | Explain split before approval, never silently truncate |
| 28 | User-selected subset and select-all | `ToggleHighlightedSelection`, `Select all ready` | Main | Selection scope always explicit; ignored/completed never bulk selected |
| 29 | Preview exact changes / backup acknowledgement | `ConfirmImportPlan`, `RunBatch` | Context confirmation | No writes before informed final approval |
| 30 | Sequential journaled metadata writes | `CatalogEnrichmentWriter` | Core | Stop on error, exact per-game intent/staging and recovery |
| 31 | Safe error/cancel handling | `CatalogBatchProgress`, `CatalogBatchPolicy` | Main progress | Clear updated/unchanged/failed counts; no false batch rollback claim |
| 32 | Existing 364 completed metadata imports | `EnrichmentJournal`, `EnrichmentPlan.IsFullyApplied` | Main Updated + History | Excluded by default; verified against live Playnite state |
| 33 | Ignored/unignored games | `CatalogIgnored`, `ToggleIgnored` | Context + Advanced | Exclusion persists and can be reversed |
| 34 | No safe changes available | `No changes available`, `EnrichmentPlan` | Main No new information | Only after enough relevant field verification; not just search summary absence |
| 35 | Full searchable/filterable history | `ShowFullHistory`, `HistoryViewFilter` | Main History / Advanced | Retain all, search, date/state filters, visibility choices |
| 36 | Hide and restore history entries | `HistoryVisibilityStore`, `HideSelectedHistory` | Advanced History | Hiding a row never deletes journal or undo data |
| 37 | Selected and latest conditional undo | `UndoSelectedHistory`, `UndoLastMetadata` | Main History | Maintain newest-first prerequisite and live-state checks |
| 38 | Revisit completed titles | `RenameSelectedHistoryTitles` | Advanced History | Keep approved title-only corrections |
| 39 | CSV diagnostic export | `Export`, `DiagnosticCsv` | Advanced | Retain data export without tokens or private paths by default |
| 40 | Additional diagnostics / evidence | `ShowTools`, `previewDiagnostics` | Context expandable + Advanced | Full score/alternatives/queries available, not required to use app |
| 41 | Recovery status / fail closed | `CatalogRecoveryConsistency`, journals | Main alert + Advanced | Block further writes until resolved, preserve evidence |
| 42 | No-change cleanup/staging safety | `CatalogMediaStagingJournal` | Core | No empty/completed journal when nothing safely changed |
| 43 | Platform/missing field filters | `platformFilter`, `missingFilter` | Main filters | Full library visibility and series-only option remain accessible |
| 44 | Existing 7 result buckets | `CatalogQueueView.Bucket` | Main redesigned grouping | No result silently disappears; retained via All/Advanced views |
| 45 | Diagnostic/legacy title-only tools | `ShowTools` | Advanced / Classic | Accessible during migration; no deletion until parity verified |
| 46 | Extension author/id and package compatibility | `extension.yaml`, `Source/Plugin.cs` | Core | Same Playnite GUID and .NET Framework compatibility; local build gate |
| 47 | Source privacy / packaging controls | `Tools/Public-Release-Check.py`, `Verify-Source.py` | Build/release | No private paths, secrets or extra files in .pext |
| 48 | First-time empty states and keyboard support | New Design C | Main | Real novice acceptance test before promoting from preview |

## Work phases and promotion conditions

- **P0 — Phase 1 (this package):** Introduce a pure read-only task classifier with focused tests. No UI or write behavior changes. This is a prerequisite, **not** a finished redesign.
- **P1 — Parallel workspace:** Add a second, opt-in `Open new Library Workspace (preview)` entry in Playnite. Classic Matcher remains available. Read-only list and inspector first, accurate count projection and full keyboard support next.
- **P2 — Real selection and preflight:** Selected Playnite GameIds only, reuse existing refresh and validation, show explicit blocked reasons and individual identity choices. No unapproved writes.
- **P3 — Approved import:** Hook the existing exact-ID checks, backup acknowledgement, journaled writer, cancellation and real history/undo. Move completion state only from verified applied journal records.
- **P4 — Parity acceptance:** Pass all rows above, novice unassisted tests, regression suite, controlled Playnite integration and rollback. Then move advanced controls and consider changing the default window.

### Acceptance scenarios that must pass before replacing the classic UI

1. First launch with no credentials provides a usable connection flow and doesn't expose secrets.
2. 504 unchanged saved results reopen with zero new IGDB requests; new ROM imported into Playnite is found without clearing the cache.
3. Select 20 saved candidates requiring preflight: only those games are refreshed; 393 other cached matches are not refreshed.
4. Previously applied 364 games are not selected automatically; previously *confirmed* identities remain eligible for missing-field review.
5. A wrong PlayStation edition cannot enter an automatic batch solely because of a score.
6. A no-change game is excluded only after relevant metadata availability has been checked; no-change creates no misleading undo item.
7. Smart-inferred identity without user confirmation can enrich eligible missing fields while preserving the original title; safe title cleanup occurs automatically for eligible approved imports and is shown before final approval.
8. A mixed manually confirmed/strict batch follows existing caps and is explained before final approval.
9. Interrupted scan/preflight restarts safely; interrupted write blocks follow-on operations and keeps durable journal/undo history.
10. All existing advanced commands, filtered history, ignored entries and CSV export remain discoverable.

**Baseline note:** Do not treat GitHub public `main` (version 0.5.17 as last inspected) as a substitute for the user's local v0.5.19 checkout. No Git commit, push, Playnite installation or metadata changes are performed by Phase 1 unless the local development installer is explicitly executed.
