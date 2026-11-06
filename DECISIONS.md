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
