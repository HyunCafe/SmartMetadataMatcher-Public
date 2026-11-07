
## v0.5.13 — Fresh-only intelligent disambiguation
- Relative matching points are not calibrated probabilities. Complete bounded exact/prefix requests are a practical coverage test, not a global IGDB guarantee.
- An existing Playnite release year may disambiguate same-title releases, but an IGDB-derived year is not an independent signal.
- Bundles, ports and remasters can be legitimate playable identities when their **full official title** matches; never treat parent_game as interchangeability proof.
- Source titles with explicit testing/edition qualifiers are never collapsed to a generic base game automatically.
- Do not persist SmartAutoEvidence in scan caches; identity selection can be revoked on fresh rescans.
- Assisted matches are blocked from auto-renaming, and write preflight checks the exact selected IGDB ID/type/platform/title.
## Decision — v0.5.11 manually verified batch (2026-11-06)

## v0.5.12 — Research first, no inferred auto approval
- Accepted: bounded read-only IGDB rival inspection using numeric candidate IDs, no new dependencies.
- Deferred: automated candidate promotion and local ROM inspection until live evidence confirms separation of competing editions.
- Explicitly preserve strict and reviewed-batch eligibility checks. A single returned candidate is not proof of catalog completeness.

**Accepted:** The user's already-marked game subset opens one full-list identity review with explicit manual confirmation instead of failing at `0 ready`. Up to 50 freshly scanned, explicitly confirmed identities may proceed through one final backup-gated approval, with read-only verification of **all** exact IGDB IDs/names/platforms before any writes. Preserve atomic identity-store persistence and fail-fast journaling.

**Preserved:** Legacy five-game supervised limit; strict-ready automation; live game title/platform checks; no automatic selection-as-confirmation; no altered IGDB similarity thresholds.

**Deferred:** Automatic approval of ambiguous/mismatched editions, new providers, and real crash testing beyond disposable fixtures.

---

## v0.5.10 decision: direct strict-ready preflight (2026-11-06)

- **Accepted:** streamline fresh strict-ready matches by offering a primary Apply verified matches action. Its only authorization is the existing scrollable final confirmation and backup acknowledgement.
- **Accepted:** preserve a separate manual Review matches path for uncertain matches and hand-picked subsets; restore Ctrl/Shift/Ctrl+A highlighting and Mark/Unmark actions.
- **Rejected:** equating identical titles or high similarity scores with confirmed identity; no relaxed IGDB platform/edition gates.
- **Deferred:** persistent no-change states, expanded metadata providers, crash-injection tests on the user's primary Playnite environment.

---

## v0.5.9 decision: display-only status separation (2026-11-06)

- Observation: a 706-game approved run reported 330 applied, 376 safe no-change skips, 0 failures. The 837-row CSV contains 101 Review, 28 Unmatched, 1 Strong and 1 Ignored. A composite `Needs review (505)` wrongly grouped the 376 no-change rows.
- Accepted: separate seven dashboard result views and sort manual review by potential field overlap, while retaining the exact same import eligibility, IGDB identity evaluation and undo logic. The no-change/unmatched/All inventory views bypass the default Series-only screen filter so they show the expected records.
- Accepted: explicitly warn for high heuristic scores when canonical titles differ, and for reported platform mismatches. This is advisory; never change trust gates or auto-approval.
- Deferred: new providers, match-threshold relaxations, re-scanning all 837 entries, schema changes or architecture refactoring.
- Required: Windows extension build, regression tests, guarded private package, optional narrow Playnite visual verification. No public release or automatic GitHub push.

## Post-v0.5.8 release gate (2026-11-06)
- Add source-only, non-destructive private release evidence tooling after synthetic recovery checks; preserve current v0.5.8 binaries and journals.
- Require exact existing `.pext` hash and DLL/YAML byte identity, pinned dependency declarations and local-only Git provenance. Verify these on Windows rather than infer them from an archive label.
- Keep independent third-party binary vetting, real Playnite crash testing and public distribution **unapproved** until separate evidence is available. Historical release notes below are retained, with the current status indexed from `RELEASE_READINESS.md`.

## v0.5.8 — Optimize only measured replay bottleneck

Benchmark measured 3000-record metadata replay at 144.42 ms versus 6.95 ms for 300 records. Use a replay-local operation-ID dictionary to remove O(n²) marker/duplicate searches while preserving full-log verification on every call. Defer media-stage session caching, compaction and recovery-state changes because they would risk stale state after interruption or external modification. Rebenchmark on Windows and retain rollback to v0.5.7.

## v0.5.7 — History view-only filtering

- Allow compact and full history to show Applied, Undone or All recorded updates. Default is All to avoid hiding any operations.
- Add title/field/date search only in Full History. Use case-insensitive substring matching, no fuzzy replacements, and do not change journal/visibility data.
- Ensure long game names and changed fields remain readable via tooltips and a selected-operation details panel.
- Filtering and hiding history are distinct: neither changes the newest-first undo policy nor deletes journal entries.
- Preserve the v0.5.0 private stable rollback tag, private-only release workflow and current metadata writer.

## v0.5.5 — Credential diagnostic boundary
- Provide an optional explicit Test Connection action that verifies both Twitch OAuth and IGDB's read-only games endpoint, instead of treating saved credentials as verified.
- No background connection at launch or save; never persist the test token, log raw HTTP bodies, send private library records, or automatically authorize imports.
- Keep existing DPAPI credential storage, pinned dependencies, backend endpoints and stable v0.5.0 rollback tag.

## v0.5.3 — Private clean no-change batch handling (pending Windows/Playnite acceptance)

- A live IGDB item with **no actual safe metadata or title changes** is now **Skipped**, not a hard batch failure. No Playnite update and no field/undo record are created; the empty media stage is durably closed clean before proceeding.
- Skip is permitted **only when no media upload and no globally shared metadata Add operation was attempted**, no field change is reported, and the clean stage closure succeeds. Anything uncertain still fails closed, stops the batch and requires recovery review.
- Batch progress and summary distinguish **Applied / Skipped / Failed**; a skipped row is unmarked and labeled `No changes available` until refreshed. Single-item imports show the same outcome without falsely reporting a completed operation.
- Preserve Playnite title/platform/identity rechecks, strict batch criteria, five supervised limit, backup approval, journal compatibility and per-game undo. No ROM, emulator, media deletion or remote changes.
- Regression coverage checks all eleven possible field flags, staged-media/global-reference uncertainty, invalid counters and correct total progress; live WPF+writer compilation and Playnite runtime remain unverified here.

## v0.5.2 — private update-history clarity and safe mixed-selection undo (pending Windows acceptance)

- The dashboard **Update history** now lists every visible committed operation with scrolling and virtualization, removing the hidden 10-row cap. Hidden entries remain accessible in Full History.
- Full History distinguishes **Undo selected metadata** (changes Playnite records) from **Show in update history** (only changes local history visibility). The latter is disabled until hidden entries are selected.
- Mixed history selections may contain Applied and Undone rows. Undo ignores already-undone operations and processes only the selected newest consecutive *applied* prefix after reading the live journal. Selected older nonconsecutive applied records are explicitly left unchanged, with counts in confirmation and result messages.
- All database safety rules remain: latest-first undo, existing manual-edit checks, global journal gate, stop on first undo error, per-game journal history, no ROM/path changes. **No existing journal data is migrated or rewritten.**
- RC version/package labels updated; automated mixed-selection regression assertions added. No Playnite UI/runtime validation is claimed until Windows build and manual check pass.

## v0.5.1 — private batch-unlock candidate (post-stable, pending Windows/Playnite acceptance)

- The v0.5.0 private-stable source at `c1b08d1` and the local `v0.5.0-private-stable` tag remain the rollback baseline.
- Remove the fixed 25-game cap for **independently strict-ready, freshly validated** matches. Select all remaining ready entries or manually choose a subset; no silent truncation.
- Keep the **five-game supervised** limit for user-confirmed/weaker-evidence matches and the one-at-a-time manual exception path. Strict evidence, cached-result refresh, duplicate game ID checks and explicit final approval are unchanged.
- Keep live exact-ID/platform/title rechecks, sequential per-game writes, stop after first failure, cancel-before-next-game, per-game undo journals, recovery blocking and Playnite/ROM isolation.
- **Do not mistake removing the testing cap for proving crash recovery or large-batch performance**. No unattended import, cross-run auto-resume, or unbounded retry is added; test a backed-up modest batch before a large one.
- This is a **private development candidate** until Windows build/offline suites and focused Playnite runtime verification pass. No GitHub publication.

## Private RC3 — Precision and export audit (2026-11-06)

- Corrected false edition warning for ordinary titles such as *Hogwarts Legacy*. Explicit `- Legacy` and `Legacy Mode` remain review-sensitive.
- Exact alias matches with a material remaster/edition/client difference now require identity review (e.g., *The Falconeer* and *Realm of the Mad God Exalt*); matched edition titles are unaffected.
- Rejects implausibly low-similarity candidate recommendations such as *THE FINALS* → *Final Fantasy IV*, retaining manual search; adds search-only article-free and collection punctuation variants.
- Candidate CSV now includes nine-field Playnite gaps at export time, count, potential fills supported by scanned artwork/description evidence, and cached-result status. Scan evidence does not guarantee a write or validate the match.
- No ROM, emulator, import writer, journal schema, five/25 batch-policy or Playnite library changes during build.
- Existing RC2 checkpoint `91148ae` remains the rollback target. Windows compilation and live IGDB search remain unverified until performed locally.

## v0.5.0 RC1 — freeze core, ship verifiable private milestone

- **Accepted:** preserve the v0.4.32 engine now that a two-game batch and clean Recovery Status were observed in Playnite; avoid speculative refactors during release candidate validation.
- **Accepted:** pin package references, verify source and packaged bytes locally, document remaining runtime gaps rather than claiming they are passed, and retain minimal manual tests.
- **Deferred:** multi-game independent undo and controlled crash injection have not yet been observed; the 25-game strict limit exists but is not a proven full-scale runtime batch.
- **Prohibited without explicit authorization:** public repository upload, credential export, ROM/emulator path changes, or cleanup of private recovery journals.

## v0.4.32 — Gate at dispatch and verify release artifacts

Source-only gates are insufficient: reject repeated Playnite game IDs immediately before import, and validate packaged bytes match the just-built DLL and approved manifest. Preserve equivalent duplicate IGDB IDs across distinct Playnite records. Keep `25 strict / 5 supervised`; a real multi-game Playnite import is still required before production claims.

### v0.4.30 — Review context invariants
- Draft edits are not catalog identity changes. Preserve review filter, highlighting and import marks when a draft is saved or reset.
- IGDB alternative selection remains a distinct, explicitly confirmed operation that may change displayed status and selected view.
- No change to media staging, data writer, recovery and supervised import caps.

- Search non-base edition suffixes only as discovery; even exact beta/launcher/legacy naming is not enough for automatic writes.
### Full-library matching audit constraints
- A perfect alias score is not sufficient when official title has no plausible lexical relationship to the original. Classify as Review; do not silently rename to a possibly different bootleg variant.
- Prefer official-title relevance to arbitrary smaller IGDB IDs when two candidates have equal match scores. Multiple equally exact same-platform records stay in Review.
- No inferred title rename or ROM/emulator-file mutations based on CSV-only diagnostics.


## v0.4.26 cached selection UX
- Display **Refresh required** as a distinct review state. Do not reinterpret persisted score/status as fresh strict evidence.
- Targeted refresh is read-only and rate-limited; never use it to automatically approve or import anything.
- Re-check the exact identity before retaining the user's selection, reject stale Playnite state, and preserve all recovery gates.

## v0.4.25 decisions
- **Accepted:** cross-ledger validation must prove exact imported media IDs match the journaled fields, not just operation/game IDs. This is a fail-closed read-only validation, with no automatic deletion/repair.
- **Accepted:** source consistency checks run before the Windows compiler, preventing repeated hardcoded-version, missing-file and orphan-test mistakes.
- **Deferred:** removing write batch limits or adding unattended processing until live Playnite import/undo/recovery tests establish safety. Do not conflate offline unit success with live Playnite behavior.
- **Preserve:** user-confirmed IGDB identity, scan cache, title cleanup, prior journals and manual-approval boundaries. No GitHub actions.


## v0.4.23 — unified final approval and recovery visibility (private preview)
- Replaced the separate backup prompt and single-line manual confirmation with one scrollable final approval window shared by manual and strict planning. User must confirm a restorable Playnite library and extension-data backup before Apply is enabled.
- Included the current Playnite title, platform, candidate, exact IGDB ID and intended Playnite title for every proposed import. The list is a planning preview; the exact live IGDB ID/platform and field state are still revalidated before writing.
- Added per-game batch progress, explicit stop-after-current wording, and a close-window safeguard while an operation runs. An interrupted/ambiguous journal remains fail-closed.
- Added Tools > Recovery status, a read-only inspection of unresolved media stages and uncommitted enrichment operations. It cannot delete or repair files automatically.
- Retained supervised five-game validation limit and per-operation journals; no production bulk unlock until Playnite import/undo and crash-recovery testing.

## 0.4.21 decision: user-approved identity memory
- Only explicit per-game approval persists an ID; similarity is not correctness probability.
- A user-confirmed match can be manually reviewed and imported but may not enter unattended strict automatic plans.
- A changed name/platform/catalog ID or edition invalidates reuse, and the writer still validates platform.
- Retain the existing batch-size safeguard until interruption testing.

## v0.4.14 — History selection and approved Playnite title corrections (private preview)

- Recent and full history tables support Ctrl-click, Shift-click and full-row extended selection.
- History can rename previously imported titles inside Playnite, with explicit confirmation for same-platform duplicate display names; it never changes ROM paths, executables, or launch configuration.
- Selective undo is limited to the newest consecutive journal operations, in reverse order; stale/manual edits fail closed.
- Unattended and supervised metadata-import collision protections remain unchanged. Build and live Playnite testing required.

## 2026-11-05: 0.4.13 visual presentation decision

Approved reference remains the generated 0.5 dashboard image; retain current safer IGDB settings pattern. Present candidate changes as proposed, not verified or approved writes. Keep exact comparison and matching evidence accessible but secondary. The scan settings button is a menu, not an expander that resizes the main toolbar. Maintain a separate live Windows UI gate before declaring fidelity.

## 2026-11-05 — v0.4.6 navigation, selection, and truthful field visibility
- Consolidated top-level Playnite menu to dashboard; historical tools remain in a Tools dropdown, not deleted.
- After writes, Completed selection takes priority over preserving a narrow search query that could hide the updated game. Apply/undo state remains journal-driven.
- New catalog field inspection is **manual/read-only**, uses exact known IGDB ID, is cached in memory for the session and does not claim catalog absence until actually queried.
- No unattended bulk writes until matching accuracy and rollback behavior are validated on a representative corpus. This iteration deliberately does not expand the writer.

# Smart Metadata Matcher — Design decisions

## Primary objective
Reduce failed metadata matches and messy displayed titles for PC and emulated games while preserving launch behavior and local files.

## Accepted
- Standalone GenericPlugin extension for Playnite 10, independent of Local Game Importer and Duplicate Review.
- Read-only preview first, then explicitly approved Playnite Name-only updates and guarded persistent undo.
- Strong deterministic rules before fuzzy matching; no claim that text similarity equals actual identity confidence.
- Status / reason, useful bulk-selection semantics, easy filters, no forced manual review of obvious repeated collection prefixes.
- Use Playnite's metadata download workflow rather than fragile undocumented UI automation.

## Deferred
- External catalog identification and provider downloads remain deferred.
- Catalog ID verification and candidate metadata lookup.
- Better aliases, regional naming, title abbreviations and user-defined rules.
- Persistent settings/ignored decisions after batch review behaviors are validated.

## Rejected for v0.1
- Rename or modify ROM files, game files, launch actions, emulator configuration or Playnite artwork.
- Unattended auto-renames and unvetted providers/APIs.
- Fuzzy name-only identity auto-approval.

## Acceptance tests for initial preview
- Valid C# compile with PlayniteSDK 6.18.0 and .NET Framework 4.6.2.
- Sample titles yield expected proposals and statuses in Tests.
- On-screen first-click marking, Ctrl/Shift/Ctrl+A, filters, refresh, copy.
- No database updates from any code path (including mark/copy/refresh).

## v0.2 decision update
- UI preview confirmed for 324 attention items in the user's Playnite snapshot.
- Explicit, confirmed Playnite `Game.Name` updates enabled in a private dev build; no automatic processing.
- Collisions blocked even for manually marked Review names; user can handle duplicates separately.
- Write-ahead undo history stored privately, not inside public repo or the .pext.
- Single-title rename/undo test required before any bulk use.


## 2026-11-05 — Goal correction after manual Pokémon FireRed metadata test

The user confirmed that the v0.2.0 explicit rename and undo worked but Playnite's
built-in automatic metadata lookup still missed games even after name correction.
**The plugin must automate most catalog identification AND metadata population,
not hand off hundreds of manually selectable metadata searches.**

Selected approach: independently query a supported game-catalog API (initially IGDB),
identify specific game IDs using title/aliases, platform and version evidence, then
implement controlled automatic missing-field population through Playnite's public APIs.
This v0.3.0 checkpoint is a READ-ONLY candidate-quality probe. The user must not be
promised unattended metadata application until that later implementation passes tests.
Scores are relative heuristics; require independent evidence and representative live
measurement before enabling automatic writes. Missing only background art is different
from unidentified game identity and should be handled separately.

## 2026-11-05 — Catalog resolver architecture checkpoint, v0.3.1
- Keep GenericPlugin and existing rename/undo; no full rewrite.
- Use one multiquery for a few bounded variants instead of many separately billed/ratelimited requests.
- Retain the original title for matching and undo; no mandatory pre-rename workflow.
- Deduplicate IGDB IDs and require independent platform/title evidence before designating Strong candidate.
- Short/generic titles and weak fallback-only matches remain Review.
- Save attempted queries, top alternatives and reason in CSV to measure false-positive risk.
- No metadata writes; test 20 known cases, then 40-60 stratified cases before authorizing automatic application.
- No public GitHub release, additional services or complex UI overhaul at this checkpoint.

## 2026-11-05 — v0.3.3 matching refinement
- Keep the working v0.3.2 direct IGDB transport; v0.3.1 multiquery is not reinstated.
- Prefer platform-compatible catalog identities when literal names match a different system, but reserve ambiguous shorthand for Review.
- Distinguish exact candidate titles from closely related sequels; a unique exact same-platform match may be Strong, but multiple exact catalog IDs remain Review.
- Use a small number of researched alias variants as search aids, NOT as catalog-identity proof. No growing per-game hardcoded exception database.
- Validate representative actual IDs and false positives before authorizing automatic metadata updates. Catalog stage is read-only.

## 2026-11-05 — v0.3.4 batch ergonomics
- Offer 20, 50 (default), or All remaining instead of fixed 20; skip custom numeric field until justified.
- Exclude clearly labeled non-production public testing clients from IGDB catalog scanning only; do not change or delete the Playnite entries.
- A scan session accumulates results across batches and exports them in one CSV; progress/cancel remain supported.
- Keep this focused on workflow efficiency while measuring resolver accuracy before implementing catalog metadata writes.

## 2026-11-05 — v0.4 controlled writer
- Resolver v0.3.4 identified 239/318 Strong candidates, but heuristic Strong != verified identity.
- First writer requires deliberate one-row selection and confirmation, re-fetches exact IGDB ID, and checks platform.
- Only missing description and cover are imported; title/launch settings/ROM files unchanged.
- Persist write-ahead journal for undo; do not enable unattended bulk until one-game write/undo is observed live.
- No second provider or automatic source changes at this milestone.

## 2026-11-05 — v0.4.1 one-game enrichment UI and recovery
- Real Playnite testing confirmed v0.4.0 description/cover import and undo; title, icon, background were intentionally untouched.
- Resolve UX friction within the existing single-game writer, not a major framework or plugin rewrite.
- Preserve original v0.4.0 metadata journal; use a separate versioned journal to track name, sorting, description, cover, icon and background together.
- In catalog apply, use exact IGDB ID/platform after explicit confirmation; catalog rename is opt-in and skipped when same-platform titles collide. Never overwrite filled media fields or custom sorting.
- Default Needs attention view hides completed rows; Completed filter shows journal-confirmed entries and remains available after reopening. Undo returns rows to active review.
- Do not require an IGDB screenshot to consider the game processed. Missing source artwork remains missing rather than fabricating art.
- Five-game and unattended/bulk writes remain deferred until one-game round-trip on the expanded fields succeeds.

### 0.4.2 design gate
- Adopt supervised batches of five using the existing one-game writer/journal as the lowest-risk path toward automation.
- Retain same-platform collision protections; titles blocked by collisions remain intentionally unchanged.
- Separate marking from selection; only explicitly marked Strong rows may be processed, after whole-batch confirmation.
- Do not equate title-similarity scores with calibrated correctness or introduce unattended All until accuracy and partial-failure behavior are live tested.
- Broader IGDB fields and final polished UI follow only after data contracts and safe rollback are established.


- 0.4.3: UI-first functional dashboard preserving 0.4.2 database writes. Local ignore records store Playnite IDs only; the History panel lists committed journal operations with no fabricated timestamps. Deliberately avoid representing unsupported metadata fields as implemented.

- 0.4.4: Approximate the approved dashboard with native WPF controls and Windows icon font; prioritize adjustable pane widths, detailed comparison, and real user feedback over invented fields or uncalibrated percentages. No external UI dependencies.

## 2026-11-05 — v0.4.5 field-enrichment decision
- The current writer lacks the fields seen in Playnite's built-in import view; add only reliably mapped fields: release year for the same platform, genres, developer/publisher companies and series.
- Shared Playnite metadata objects are retrieved or created by name; undo restores the game's references and does not delete global shared catalog entries.
- Preserve existing metadata; leave ratings, tags, features, categories, region, age ratings, install size and original platform unchanged until mappings and expectations are explicitly verified.
- Do not conflate an IGDB first-release date for a different platform with this ROM's actual release date.
- Theme fidelity remains a separate follow-on task after functional import checks. Do not claim the generated design mockup is functional.


### 0.4.7 — authorization boundary and stopping rule
- A Strong score is heuristic, not sufficient for unattended writes. Automated planning only, with an explicit scrollable approval list (up to 25).
- Do not repeatedly rewrite metadata or game files; preserve user's already populated fields. Durable per-game journal and conservative identity rechecks stay mandatory.
- Same-platform title collisions, repeated IGDB IDs and unknown/ambiguous platform/edition evidence remain supervised-review cases.
- Keep the existing 1–5 supervised selection path; defer true unattended/entire-library writes until a representative audited sample and complete live undo verification demonstrate reliability.

## v0.4.10 — marked review and supervised test UI

- Review dialog opens on marked games when available, otherwise ready games or all matches; no unexplained empty first view.
- Inspect button has an auto-selected row, disabled state for empty categories, and explanatory no-selection messages.
- Strictly ready marked games can be forwarded to the **existing** exact-ID revalidated batch writer only after a Playnite backup confirmation and full second approval. Private testing limit is five marked games; no silent truncation.
- This is NOT the v0.5 durable unified job runner, not an accuracy certification, and not permission for production bulk imports.
- The Playnite metadata writer, journal format, IGDB transport, installed extension, and public repositories are unchanged by this source patch.

Manual exception: exactly one marked Strong candidate may be tested via the existing manual writer after separate identity confirmation, backup confirmation and final write approval. Multiple marked games require strict readiness.


### 0.4.11 — CSV-driven title retrieval and identity safety (private preview)
- Audited **Super Mario Adv** on GBA resolves to Super Mario Advance; no generic `Adv` trust.
- Verified redundant acronym removal (e.g. `WDL - World Destruction League`), preserving nonmatching acronyms.
- Exact complete-token permutations enable reordered edition/main-title matches while retaining every digit; unique same-platform catalog identity and dated record remain mandatory for automatic planning.
- Search-only fallbacks for titles ending in `Adv` (Adventure/Adventures), informal `1` numbering and numbered series subtitles. Search-only variants cannot independently authorize strict automation.
- Fire Dragon (undated), bare Mario Kart, wrong-platform Pokemon Yellow, rival sequels and duplicate exact IGDB records continue to require review.
- The matching evidence is still not calibrated probability; **no modifications to enrichment writer or journals**. Live IGDB quality and Playnite tests are pending.

## 2026-11-05 — v0.4.12 UI consolidation
- Accepted: integrated review marking/search, unified primary actions, collapsible scan options and empty history, initial preview selection.
- Deferred: durable job execution, per-field verified preflight, persistent scan/ID decision cache, and automatic bulk writing pending live accuracy/recovery testing.
- Existing writer paths remain available for supervised testing only; version 0.4.12 is not production.

## v0.4.15 decisions
- Keep one v2 journal and preserve backward compatibility, including undated historical records.
- Read-only history may show undone records; only active latest-consecutive operations can be undone.
- Existing manually approved title correction retained; no unattended duplicate-title renaming added.
- Do not change the metadata writer or image staging in the same batch as history schema evolution.

## 0.4.16 decision: history removal is presentation-only
- Hide selected events with a private GUID-only preference, never erase write-ahead undo evidence. Full History can show/restore hidden rows.
- Verified same-platform title duplicates receive an explicit platform suffix; unresolved second collisions require manual review, not invented numbering.
- Bulk/unattended title collision behavior remains unchanged. Only supervised rename uses the suffix proposal.

## v0.4.17 workflow decision
- Default user journey is Scan → Review identity and planned changes → Approve → Name and missing metadata updated together → History/Undo. Title correction must not require visiting history.
- Strict identity eligibility is independent of naming collisions. A duplicate produces a readable platform suffix when unambiguous, never an arbitrary counter.
- Common UI stays simple; diagnostic internals remain secondary.

## v0.4.18 UX simplification
- Common actions near their content: scan at top; select and review beside the results table; advanced actions in menus. Highlighted rows and import checkboxes remain intentionally distinct, with explicit tooltips.
- One context-aware select/deselect button replaces separate mark/unmark; second toggles visible ready rows within current filter. No hidden expansion into the entire library.
- Review dialog must label the ready state as subject to identity verification; supervised write cap and approvals remain until production recovery is validated.

## v0.4.19 decision
- Prefer direct visible action buttons over opaque More menus. Keep batch selection, explicit review/approval, and reversible remove-from-recent semantics.
- Missing release year is not automatically a mismatch. Require exact unique title, structured platform ID, and substantive summary to admit undated games into the user-approved strict plan. Reject platform conflicts and repeated identical catalog identities.

## v0.4.20 continuity decision
- Reuse recent read-only candidate data rather than repeating 318 network searches on each plugin opening. Live candidate data expires after seven days and is invalidated by changes to title/platform/missing-field count.
- Never persist credentials or mark selections in the catalog scan cache; saved candidates cannot qualify for strict automatic planning, even if their old score was strong. Exact live IGDB ID revalidation is still required before supervised writes.
- User-requested fresh scan archives the previous cache and does not touch Playnite game files, database, metadata histories, or committed undo journals.
- Defer long-running unattended bulk imports and jobs until crash/media-recovery guarantees are verified; do not treat cache as user-approved identity.

## v0.4.22 recovery decision
Use a separate durable media-staging ledger instead of rewriting legacy field-undo journals. Begin must precede Playnite AddFile; each returned media ID must be logged before the next step. Prepared enrichment journal owns the recorded IDs after handoff. An unknown ID in the unavoidable AddFile-return interruption window cannot be safely guessed: fail closed and require backup-backed manual recovery. Defer fully automatic recovery until real Playnite process-interruption tests.

## v0.4.24 decision checkpoint
Accepted: cross-journal safety validation, fail-closed orphan/mismatch detection, clear non-guaranteed field preview and unchanged undo format.
Deferred: automated media cleanup, unrestricted bulk imports, cross-run execution resume and GitHub release until disposable-profile recovery testing.

### v0.4.27 duplicate identity policy
Allow multiple distinct Playnite records to match one IGDB record when the catalog title and normalized platform agree; this is not, by itself, suspicious. Do not weaken individual strict evidence or exact-ID/platform revalidation. Distinct catalog titles or conflicting platforms for a repeated ID still require review. Distinguish duplicate display names automatically with stable console suffixes, keep approved import previews and Playnite write validation aligned, and never touch the underlying ROM filename or launch path. No automatic import authorization.

### v0.4.28 review context actions
Use Playnite's supported MainView.OpenEditDialog for editing any game metadata rather than recreating its editor or writing unjournaled fields directly. Clearly distinguish manually saved Playnite edits (outside plugin undo) from supervised Matcher imports. Offer read-only exact IGDB inspection and title search inside the Matcher; only adopt matching-platform search results after explicit identity confirmation and preserve live ID/platform preflight and final import approval. Right-clicking must not toggle import marks.

## v0.4.29 — Staged metadata changes
- Only title and description are editable as staged text. Other missing fields can be individually included/excluded; genres/developers/publishers/release editing stays in Playnite native editor, outside Matcher undo.
- Staged drafts are in-memory and tied to the exact Playnite game name/platform and IGDB ID/title. Closing Matcher discards unapproved drafts. Saving a draft never changes the library.
- The only path to write a staged draft is the same explicit final approved import and write-ahead enrichment journal.
- Custom titles must be unique by normalized display name; unsafe or changing identities fail closed.

## v0.4.31 — Batch promotion after one-game import/undo validation
- User confirmed an actual Playnite Driv3r metadata import and successful metadata restore on v0.4.25–0.4.29. Real crash/interruption and larger batch sizes are not yet verified.
- Promote **only** fully strict-reviewed entries (not user-confirmed or cached Strong labels) to the existing 25-per-approval path, with its live revalidation and recovery checks. Keep manual/confirmed matches at five; all writes remain explicitly approved.
- Stop a running batch on its first error, regardless of whether that error damaged a journal; this makes partial completion easy to inspect and undo. Do not automatically retry failures or touch ROMs.
- This is a private preview with a bounded opt-in batch; do not claim production-scale safety until successful multi-item runtime testing.

## RC2 correctness scope
- Reuse the existing catalog cache and staging journal formats; no extra transaction engine or shared-object auto-deletion. Legacy caches remain read-only and untrusted for writes.
- If a shared Genre/Company/Series Add() was attempted and pre-journal processing fails, keep the operation unresolved, blocking further imports until backup-backed review. This is a safety improvement, not an automatic recovery mechanism.
- Preserve the previously verified title/planning/undo writer behavior; no public upload.

## RC4 decisions (private, 2026-11-06)

- Retain one conservative Strong gate and make low-similarity structured-platform anchored matches Review-only.
- Use one bounded exact-name API fallback only on Unmatched results, not on every game.
- Filter Series-only candidates in the UI by default without dropping them from the queue/cache/export.
- Do not refactor the metadata writer, undo, recovery, or journals to address discovery regressions.

## v0.5.4 private — action labels, not new trust rules
A cached *Strong candidate* remains saved, read-only identity evidence and may not become strict-import-ready until refreshed. The main grid now says **Refresh required**, while the internal `Status` and `AutoPlanEligible` flags remain untouched. Select-all actions describe their actual scope. No auto-approval, API, writer or undo change was approved in this UX milestone.

## v0.5.6: reduce worst-case scan progress loss without altering identity safety

Use the existing atomic read-only cache after every 10 completed scans; flush the short tail at normal scan termination, cancellation or provider error. Do not replace the cache schema or make cached matches eligible for strict import without live refresh. No one-file-per-game writes, separate database, new dependencies or implicit Playnite writes. A hard shutdown can still lose at most the uncheckpointed short tail (assuming the last file replacement reached durable storage); post-crash filesystem durability is not guaranteed. Cache failures must preserve in-memory progress and surface a warning rather than abort read-only discovery.

## Post-v0.5.8 recovery fault-matrix decision

Measure and assert documented fail-closed behaviors with synthetic durable ledger events before refactoring import/undo recovery paths. Exercise actual existing journal classes against disposable files, without invoking Playnite AddFile or modifying its live database. A passing synthetic matrix does not establish atomicity between Playnite and the two journals or prove real process-crash recovery. Keep production sources and extension version 0.5.8 unchanged.

### v0.5.11 manual review flow (private candidate)

In **Review matches**, mark 1–50 freshly scanned game suggestions and select **Confirm identities & review**. A scrollable list displays each Playnite game, platform, IGDB title/ID and evidence. Check the explicit acknowledgement **only after personally verifying every identity**, then continue to the separate final field/backup approval. The plugin saves the identity decisions atomically; no metadata changes occur until the final approval. For the larger reviewed flow, **all** exact IGDB IDs, names and platform memberships are checked live before the first write; any failure stops the entire batch without applying it. Later write errors still stop at the first failing game and preserve individual undo records for previous successes. The existing legacy five-game supervised path is unchanged.

Do not bulk confirm a candidate whose platform, edition or title you have not personally verified. Individual inspection remains available. This candidate has not yet been tested in Playnite.

### v0.5.13 optimized evidence and numerals
- Only title ordinals convert Roman/word-number forms; ordinary pronouns, crossover X, and distinct franchises do not.
- Duplicate IGDB ID search records retain their fullest fields before ranking.
- Unknown provider game type stays Review; explicit playable episode and standalone expansion names may rank.
- Rating-count dominance is a catalog heuristic, not an 80% calibrated probability or independent local identity.
- Synthetic fixtures are not a substitute for authenticated replay against the 100 real Review cases.
- Plus/++ and premium qualifiers may represent distinct releases: preserve them when comparing provider identities.
