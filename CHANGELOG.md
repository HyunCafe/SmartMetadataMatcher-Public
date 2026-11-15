## v0.5.70 — Bounded review selection feedback (private preview)

- Prevent bulk Mark highlighted and Mark all shown from selecting more than the existing 1000-game review limit; reject the entire over-limit action without partially changing marks.
- Prevent individual Mark checkboxes from exceeding that limit, and show an actionable message instead of silently dismissing an invalid Review request.
- Preserve current marks, all IGDB exact-identity and approval gates, shared importer, journals, undo, and Classic fallback. No Playnite metadata writes performed by marking.
- Pure C# boundary cases and source guard regressions added. Real Playnite/WPF behavior and import/undo/recovery remain unverified.

## v0.5.69 — Review plan interaction clarity (private preview)

- Make both read-only review grids non-selectable by mouse, remove alternating/highlight-like row fills, and preserve scrolling and full-text hover tips. The main game list and IGDB identity chooser remain selectable.
- Simplify review instructions and state the return-to-review / field-plan / Apply path; do not imply that merely reading or selecting a proposal approves it.
- Preserve exact IGDB verification, explicit backup/final approval, automatic safe Playnite display-title cleanup, shared writer, recovery/undo and Classic fallback unchanged.
- Source tests are not WPF or Playnite acceptance; Windows build, live navigation, import, undo and recovery remain to be verified.

## v0.5.68 — Contextual identity recovery (private preview)

- No-match games now open the existing exact-ID IGDB search instead of a blocked review. A chosen suggestion returns to the same focused/marked review scope; review marks are not import approval.
- Focused review/double-click/context menus expose **Find IGDB match** when appropriate; unscanned games receive a clear scan-first message.
- Preserved exact identity confirmation, live fields, final approval, writer, journal, undo and Classic fallback. Continuity records only current unresolved work.

## 0.5.67 — Cohesive Design C workspace presentation (private preview)

- Prioritize **Review marked games** in the library selection tray while preserving explicit marking, unmarking, bulk triage, search, and per-GameId review scope.
- Turn the inspector into a task-oriented panel: primary focused review, live IGDB field comparison, and optional draft choices appear together; lesser-used refresh, identity-management, ignore/restore, and saved evidence live in a clearly labeled expandable section. The original event handlers and eligibility checks are retained.
- Separate identity and field-check prerequisites from final Apply / Classic / Close actions in the existing review dialog. Reduce the one-game review's unused grid height without changing any import preflight, title planning, approval or backup gate.
- Simplify History copy and group view-only hide/restore under a single Visibility menu; emphasize conditional Undo Latest while keeping newest-first selected undo and recovery inspection.
- Keep the dark blue/cyan identity and existing keyboard and context-menu shortcuts. No importer, journal, cache, credential, recovery or Playnite database write logic was modified.
- Source regression tests only. Windows/WPF appearance and real Playnite import/undo/recovery remain separate acceptance gates. Classic stays available.

## 0.5.66 — Compact review presentation (private preview)

- Use one compact review layout for either a focused game or a single marked game; keep exact ID scopes isolated.
- Condense the batch overview from ten overlapping columns to seven task-driving columns, with complete cell text still available on hover and exact field details in the existing verified plan.
- Show concise ordered review instructions, hide unnecessary identity confirmation when all identities are confirmed, and reveal the field-by-field plan button only after a complete exact-field check.
- Prevent decorative click selection in read-only review rows. Preserve scrollbars, tooltips, original verification, approval, writer, history, undo, recovery, and Classic fallback.
- Real Playnite metadata import, undo, and recovery remain unverified release gates.

## 0.5.65 — Quick focused review navigation (private preview)

- Double-click a saved eligible game row to open the existing focused review directly, without marking it or creating a new import path; a game without a usable saved match still opens its familiar read-only details.
- Ignore double-clicks on Mark checkboxes, blank table space and headers so routine marking never opens a review unexpectedly.
- Preserve the full read-only game-details popup under the row context menu, and provide Ctrl+Enter as a keyboard route to the same focused review. Existing Ctrl+M marking and batch review remain distinct.
- Preserve exact GameId scoping, live identity and field preflight, explicit approval, automatic safe display-title cleanup, shared writer, journals, undo and Classic fallback unchanged.
- Tested by source regression guards only; Windows build, Playnite navigation and real metadata import/undo/recovery remain separate verification gates.

## 0.5.64 — Contextual review workflow consolidation (private preview)

- Show one context-appropriate **Apply** action for focused-game or marked-batch review; the existing native single/batch import paths, all preflight gates, and final confirmation remain unchanged.
- Present a compact one-game review with current/proposed display title, exact identity status, and next step rather than a ten-column horizontal-scrolling batch table; retain the full multi-game details and separate verified field-by-field plan.
- Make the native Design C action primary, keep Classic as a secondary fallback, disable redundant identity review when all selected games are already confirmed, and shorten contextual instructions.
- Correct live-check completion wording so it no longer insists Classic is the sole path. Never treat read-only field checks or cached suggestions as import authorization.
- New targeted source assertions verify review scope, focused/batch action separation, and unchanged writer boundaries. Real import, undo, and recovery remain deferred public-release gates.

## 0.5.63 — Automatic title cleanup and functional consolidation (private preview)

- Remove the last internal Classic title-cleanup checkbox and its state dependencies. Eligible approved imports always plan safe Playnite display-title cleanup, with no additional user option.
- Retain exact-title preview, explicit final approval, duplicate/edition and custom-title safety. Smart-inferred identities without manual confirmation continue to preserve their original titles.
- Refresh the legacy 48-item parity gate as a current preservation checklist, not an obsolete to-do list; clarify that native code paths are present but real import, undo, and recovery remain unverified.
- Preserve the existing single writer, recovery journals, Classic fallback, and guarded source/build workflow. No new metadata-writing path.

## 0.5.62 — Focused-game review (private preview)

- Review one selected saved IGDB candidate directly from the inspector or right-click menu, without first marking it or including other already-marked games.
- Reuse the exact existing native/Classic review, live-field preflight, approval, draft, journaled import and undo paths; reviewing does not authorize a write.
- Keep the multi-game marked-review action unchanged; correct its tooltip to acknowledge native Design C imports.
- Focused review is unavailable without a valid saved IGDB identity, and is blocked during active scans, field checks or imports.
- Do not mark deferred live import/undo/recovery acceptance as passed. Classic remains available.

## v0.5.61 — Safe workspace transitions and recovery navigation (private preview)

- Prevent opening Classic during *any* active Design C scan, single/batch field check, or native import. Show an actionable message rather than silently ignoring the request.
- Respect a canceled/blocked WPF window close before launching Classic, including selected-review handoff, so an active workspace cannot coexist with an unintended second matcher.
- Point recovery warnings to the native read-only recovery inspector under Advanced; retain Classic for legacy recovery and preserve all write/undo gates.
- No changes to matching, credentials, import planning, approved writer, journals, undo, ROM paths, or release identity.
- Real Playnite import, undo and recovery acceptance remains unverified and is not a prerequisite for this private UI safety patch; Classic cannot be retired yet.

## v0.5.60 — Design C interface copy and draft discoverability (private preview)

- Corrected obsolete "Classic-only" import descriptions: Design C has its own verified single-game and batch approval paths; Classic remains an explicit advanced fallback while runtime acceptance is pending.
- Changed the draft action to "Choose fields / custom text", explained that IGDB field checkboxes select catalog values rather than edit them, and moved optional custom title/description text boxes above the field list.
- Shortened the crowded marked-game review introduction without removing exact-identity, field verification, backup acknowledgment or explicit final approval requirements.
- The Playnite menu no longer calls the active Design C workspace "read-only". No import, undo, journal, credential, or media-write code changed.
- Runtime write/undo acceptance is **unverified at user request**; do not promote to public release or retire Classic on that basis.

# 0.5.59 — Build workflow continuity update (tooling only; installed extension unchanged)

- Bring the guarded preview builder into the source checkout as `Tools/Build-Preview.py` and optional `.cmd`; build, regression-test, package, checksum-check, archive lower-version previews, then directly open `.pext` on request (no Explorer/Documents popup).
- Retain local maintainer workflow and handoff state outside public source distributions; public build instructions are in `CONTRIBUTING.md`.
- Add disposable preview-packaging regression fixtures and the pending Design C real Playnite import/undo/recovery acceptance procedure. Do not promote Classic retirement until actual host behavior is verified.

# 0.5.59 — Read-only field preview and draft-editor guidance (source candidate)

- Keep the proposed-field table explicitly read-only: pointer clicks do not select or highlight rows, while its scrollbars and tooltips remain available.
- Place the existing draft editor above the comparison, with a visible prerequisite and disabled-button tooltip until a fresh exact-ID field check completes.
- Preserve session-only draft validation, separate identity confirmation, journaled import, explicit final approval, History/undo, and Classic fallback unchanged.
- Windows build, Playnite UI behavior, real imports and undo require separate verification; do not promote this candidate based solely on source checks.

# 0.5.58 — Design C library triage and console platform coverage

- Exact console IGDB platform-ID aliases for common PS3, Nintendo, Sega and Xbox emulators; unknown/mixed platforms remain manual-review only.
- Filter by Playnite Source using the existing platform selector; source is display-only and cannot authorize an import.
- Show meaningful missing-field gaps or Series-only gaps; scan meaningful gaps first without excluding any eligible game.
- Retain the shared exact-ID importer, final approval, title cleanup, journals, undo and Classic fallback.

## 0.5.57 — Bulk triage in Design C

- Add filtered-view Mark shown / Unmark shown for eligible games; mark state is never import consent.
- Add highlighted batch Ignore / Restore via one atomic existing ignored-ID preference write, after explicit review and live game-state checks.
- Preserve cached matches, user-confirmed identities, journals and Playnite metadata; reject mixed/stale selections without partial changes.
- Keep the four commands and quick **Show ignored games** filter in a compact Bulk actions menu to avoid another permanent toolbar row.

## 0.5.56 — Design C parity and review-access bundle (preview)
- Native History supports search, applied/undone filters, and shared hide/restore visibility preferences; no journal or metadata is changed by visibility actions.
- Advanced exposes the existing full-library diagnostic CSV exporter, plus a keyboard/review shortcut reference.
- Right-click selected games for native IGDB field comparison, identity correction, draft editing, ignore/restore, History, and clipboard actions; all use existing guarded commands.
- Import, approval, journaled writer, title-cleanup, and undo logic unchanged; Classic retained for remaining legacy tasks.

## 0.5.55 — Native selected recent undo

- Design C History can reverse an explicitly selected contiguous newest-first prefix of applied imports.
- Itemized approval, backup acknowledgement, journal/recovery revalidation per operation and fail-fast partial-result reporting.
- Reuses the established undo writer; newest-only undo and Classic legacy fallback remain. No ROM files or executables changed.

## 0.5.54 - Native Ignore/Restore in Design C
- The focused game can be ignored/restored directly from the Design C inspector with confirmation.
- Reuses CatalogIgnored private ID store; saved match/identity retained; never edits Playnite games or ROM files.
- Clears stale marks, drafts and session-only strict evidence; preserves filters and shows relocated game.
- Adds offline native preference-state regression coverage and retains Classic fallback.

## 0.5.53 - Native History/Latest Undo
- Design C exposes the newest completed metadata import undo, with backup acknowledgement and fresh recovery-ledger recheck.
- The original journaled writer remains authoritative; legacy and older-history recovery remain in Classic.

# Changelog

## 0.5.19 — direct cached-refresh action (private preview)

- Add a visible Refresh required (N) button in the main toolbar.
- Refresh only cached strong matches requiring new IGDB evidence; other pending games
  and previously completed/ignored/user-confirmed games remain untouched.
- Reuse the existing cancellable read-only selective-refresh worker, with a
  confirmation before large batches; do not approve or apply metadata automatically.
- Add regression cases for exact refresh-required classification.

## 0.5.18 — incremental scan preview (private test candidate)

- Keep valid unchanged read-only IGDB scan results beyond seven days; only new games and
  records invalidated by title, platform, local release year, or missing-field changes require new lookups.
- Separate saved-result counts from pending IGDB lookups in the dashboard summary.
- Preserve explicit Tools > Rescan all behavior, which archives previous saved results.
- Cached identities never regain automatic metadata-write approval; live validation
  and user-reviewed decisions remain mandatory.
- Add regression tests for long-term reuse, new ROMs, changed title/platform/missing-count/year.

## Unreleased

- Replace game-specific matching overrides with shared title, platform, ordinal, edition and rival-evidence rules; preserve meaningful numbers and `+` markers.
- Expand discovery hints without allowing stripped prefixes or platform tags to establish identity automatically; add generalization and negative fixtures.
- Consolidate private and public histories into the existing public repository, retaining original commits and development records.
- Add Windows build/test/candidate-package automation, contribution guidance and issue forms; reconcile current release status and maintain the versioned design-decision history.

These source changes are not included in the existing beta package.

## 0.5.17-beta.1 — experimental public beta

- Expand manually reviewed identity-confirmation batch from 50 to 1000 games, retaining explicit whole-selection confirmation and live preflight before any writes.
- Allow all strict-ready candidates to be marked even when previously confirmed selections exist; no silent truncation to five.
- Present IGDB identity-preflight progress in 50-game groups. Grouping is display-only; per-game recovery journals remain unchanged.
- Add boundary and persistence tests for 51, 431, 831, 1000 and 1001 selected identities.
- Prepare release-source, security/privacy documentation and separate Playnite add-on manifest templates.

## 0.5.16 — private development build

- Prevent checkbox double-clicks from closing the Review matches window.
- Show live identity-preflight progress for manually reviewed batches.

## 0.5.15 — private development build

- Improve PSP paired-compilation and original/port matching distinctions.

The reviewed Git histories and historical development records are now retained in this repository. Historical entries describe their original checkpoints, not current release approval.

## Earlier development

The [complete development changelog](Docs/DevelopmentHistory/CHANGELOG.md) preserves the private-development entries.
