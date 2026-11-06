- Added search-only recovery for titles ending in Legacy/Beta/Test/Playtest/Starter Access/Launcher/SDK and + DLC; ambiguous editions never become Strong automatically.
### Full 318-game export audit
- Detected 244 Strong, 48 Review, 24 Unmatched, 1 Completed, 1 Ignored entries.
- Three unrelated-name exact aliases (Bump Bomb / Manic Troll / Hanoic Tower) were previously Strong; now forced to Review unless independently confirmed.
- Resolve equal-score IGDB candidates by official catalog title relevance before provider ID; ambiguous same-platform identities still require manual review.
- Cached Strong results gain targeted read-only Refresh selected without being auto-approved from persisted score labels.


## 0.4.26 — Targeted cached-result validation
- Review explains why saved Strong labels cannot immediately enter strict multi-game imports.
- Read-only **Refresh cached** action re-queries only selected results instead of clearing the entire catalog cache.
- A refreshed result preserves its import checkbox only if both IGDB ID and edition title are identical; changed identities are automatically unmarked.
- Live Playnite title and platform must still match the saved context. Existing writer, five-game supervised cap, staging journal, undo and ROM paths are unchanged.

## v0.4.25 — media ownership verification and release checks (private preview)
- Correlate actual cover/icon/background staging IDs with the exact field-level undo journal owner. Failed/corrupt media ownership blocks future writes, including after undo; legacy field-only journals remain compatible.
- Add offline regressions for three valid artwork slots, mismatched/missing IDs, orphan stage slots, persisted replay, historical undo and legacy entries.
- Add a read-only prebuild verifier covering source/manifest version consistency, runtime UI titles, project compile inventory and invoked offline tests.
- Keep the five-game supervised limit and all existing writer paths unchanged. Live Playnite import, undo and crash recovery remain unverified.


## v0.4.23 — unified final approval and recovery visibility (private preview)
- Replaced the separate backup prompt and single-line manual confirmation with one scrollable final approval window shared by manual and strict planning. User must confirm a restorable Playnite library and extension-data backup before Apply is enabled.
- Included the current Playnite title, platform, candidate, exact IGDB ID and intended Playnite title for every proposed import. The list is a planning preview; the exact live IGDB ID/platform and field state are still revalidated before writing.
- Added per-game batch progress, explicit stop-after-current wording, and a close-window safeguard while an operation runs. An interrupted/ambiguous journal remains fail-closed.
- Added Tools > Recovery status, a read-only inspection of unresolved media stages and uncommitted enrichment operations. It cannot delete or repair files automatically.
- Retained supervised five-game validation limit and per-operation journals; no production bulk unlock until Playnite import/undo and crash-recovery testing.

## 0.4.21 — user-confirmed IGDB identities (private preview)
- Review matches: confirm/forget a highlighted exact IGDB ID for one unchanged Playnite game.
- Private atomic, size-limited JSON decision store with invalidation on game title/platform or catalog identity changes.
- Reuse confirms after cache restoration or fresh scan without granting strict unattended eligibility.
- Manually confirmed groups may pass supervised (max 5) import only after live ID/platform check, final approval and journaling.
- Malformed identity file is preserved and saving confirmations is blocked until resolved.
- Metadata writer/ROM files/undo journal unchanged.

## v0.4.20 — Resumable read-only catalog scans (private preview)
- Persist recent IGDB candidate summaries in private plugin data after each finished or cancelled scan batch; restore them on reopening the dashboard.
- Cache matches only when exact Playnite game ID, name, platform and missing-field count agree and the saved scan is at most seven days old.
- Cached candidates never receive strict automatic-write eligibility; new import operations must revalidate the catalog ID live.
- No import checkboxes/marks are restored. Malformed cache is preserved and scan can continue read-only.
- Tools > Rescan all archives any existing cache, resets pending scan work and leaves Playnite metadata, journals, ROMs and history unchanged.
- Added offline tests for persistence, expiry, invalidation, UTF-8 data, malformed-state preservation and fresh-scan recovery.

## v0.4.14 — History selection and approved Playnite title corrections (private preview)

- Recent and full history tables support Ctrl-click, Shift-click and full-row extended selection.
- History can rename previously imported titles inside Playnite, with explicit confirmation for same-platform duplicate display names; it never changes ROM paths, executables, or launch configuration.
- Selective undo is limited to the newest consecutive journal operations, in reverse order; stale/manual edits fail closed.
- Unattended and supervised metadata-import collision protections remain unchanged. Build and live Playnite testing required.

## 0.4.13 — Visual dashboard refinement (private preview, 2026-11-05)

- Kept IGDB credentials in Settings, aligned status in a compact header.
- Replaced inline Expander with a single scan-settings dropdown (20/50/all and collision-safe rename setting).
- Separated main Scan/Review actions from Undo, Export, Tools and Close; improved category tab visual weight.
- Redesigned preview with larger artwork, potential field changes, collapsed detailed comparison/diagnostics, and shorter safety summary.
- Main table and selection filters rebalanced; empty history remains compact.
- Matching, metadata writer, journal format, approval paths, and network transport unchanged. No bulk-automation safety gate changes.
- Verification pending: Windows build and real Playnite UI at user DPI.


### 0.4.11 — CSV-driven title retrieval and identity safety (private preview)
- Audited **Super Mario Adv** on GBA resolves to Super Mario Advance; no generic `Adv` trust.
- Verified redundant acronym removal (e.g. `WDL - World Destruction League`), preserving nonmatching acronyms.
- Exact complete-token permutations enable reordered edition/main-title matches while retaining every digit; unique same-platform catalog identity and dated record remain mandatory for automatic planning.
- Search-only fallbacks for titles ending in `Adv` (Adventure/Adventures), informal `1` numbering and numbered series subtitles. Search-only variants cannot independently authorize strict automation.
- Fire Dragon (undated), bare Mario Kart, wrong-platform Pokemon Yellow, rival sequels and duplicate exact IGDB records continue to require review.
- The matching evidence is still not calibrated probability; **no modifications to enrichment writer or journals**. Live IGDB quality and Playnite tests are pending.

### 0.4.10 — UI selection/test-preview correction

## v0.4.10 — marked review and supervised test UI

- Review dialog opens on marked games when available, otherwise ready games or all matches; no unexplained empty first view.
- Inspect button has an auto-selected row, disabled state for empty categories, and explanatory no-selection messages.
- Strictly ready marked games can be forwarded to the **existing** exact-ID revalidated batch writer only after a Playnite backup confirmation and full second approval. Private testing limit is five marked games; no silent truncation.
- This is NOT the v0.5 durable unified job runner, not an accuracy certification, and not permission for production bulk imports.
- The Playnite metadata writer, journal format, IGDB transport, installed extension, and public repositories are unchanged by this source patch.

## 0.4.9 — read-only plan review and dashboard simplification (private preview)
- Windows build not yet verified. No host/runtime verification or install claims.
- Added read-only Review plan dialog: strict-preflight-ready, needs-review, and completed/ignored classifications; identity evidence, rename handling; focused navigation to dashboard.
- Moved IGDB credentials to local Settings dialog; DPAPI storage unchanged and saving does not validate connection.
- Replaced overlapping primary 1/5/25 write buttons with Review plan. Original supervised write actions preserved under Tools > Legacy supervised testing pending durable unified job runner.
- Technical score/ID/alternatives/reason columns hidden by default, optional via Tools. Existing diagnostics and CSV exports unchanged.
- No metadata writer, IGDB query, journal, candidate engine, or test engine changes. No new remote/publishing features.
- Next: Windows build and UI smoke check, then durable job runner, exact field-level preview, recovery.

## v0.4.8 — PRIVATE engine foundation checkpoint (target v0.5 redesign)

- Replaces displayed explanation-string parsing as the strict auto-plan authority with typed resolver evidence; matching threshold and platform/year guards remain unchanged.
- Introduces a pure, independently testable job-plan builder: explicit reasons, duplicate IGDB IDs across all currently known scanned records, optional rename separated from metadata eligibility, and marked-scope modeling.
- Strict auto-plan now consumes the plan builder; still uses v0.4.7's conservative 25-at-a-time confirmation and existing per-game writer (temporary transitional UI).
- Blocks new single/batch enrichment if the global v2 journal contains an unresolved write or cannot be read; stops the rest of a batch if an exception leaves an unresolved journal entry.
- Aligns manifest, assembly version and window title to 0.4.8. No new persistence, SDK writes, automatic resume, new dependencies, live validation or public release.
- Known remaining P0 work: add recoverable media/reference staging prior to journal Prepare; fail-closed tests for crash between Prepare/Update/Commit; one coherent v0.5 plan/apply UI.

## 0.4.7 — reviewed strict auto-plan (opt-in)
- New **Auto-plan next 25...** action: chooses up to 25 freshly scanned candidates using structured IGDB platform IDs, unique exact/alias matches and known release year; no automatic writes without a separate full-list approval window.
- Exclude same-platform title collisions, repeated IGDB IDs, ignored/completed games and previously failed operations. Only missing fields are imported; existing metadata, ROMs and launch configurations remain unchanged.
- Revalidate exact catalog identity and platform before each import; sequential writes, progress, cancel-before-next, and per-game durable undo retain the existing writer.
- Previous manually selected 1–5-game workflow remains unchanged. A Strong/1.000 score is NOT a statistical probability and does not itself permit bulk changes.
- Additional offline tests cover strict auto-plan eligibility, bad/missing platform IDs, aliases and ambiguous records.
- This is an opt-in preview; verify full Playnite backup and live extended-field undo before approving 25 games.

## 0.4.6 — consolidated dashboard and field inspection (UI-only, read-only diagnostics)
- One Playnite Extensions submenu action, **Open Smart Metadata Matcher**. Other existing tools remain available through the dashboard's Tools dropdown.
- After a confirmed one-game or small-batch metadata import, automatically switch to Completed and keep the newly processed game selected instead of blanking the preview. Restore focus after undo.
- On-demand exact-ID **Inspect IGDB fields** in the preview shows real genres, developers, publishers, series and matching-platform release date, with explicit Unavailable / Preserve existing / Fill missing handling. This inspection makes a read-only API request only when clicked.
- Existing title-review, full diagnostics exporter and import history are preserved. No new metadata writes, automation policy, or changes to Playnite file paths were introduced.
- Existing 1–5-game explicit confirmation, field-preserving undo, collision protection and credentials-in-DPAPI remain unchanged.

## 0.4.5 — richer safe metadata enrichment (development preview)
- Fetch platform-specific release years, genres, developer/publisher companies and series from IGDB by verified game ID.
- Fill each field only when missing; do not modify existing values or platform assignments.
- Extend the persistent metadata journal, completed checks and undo to cover new fields, preserving older entries.
- Raise dashboard text/control sizing slightly; comprehensive visual redesign deferred until this data path is verified.
- Keep confirmed 1–5-game limit, dry-run scanning, review protection and existing ROM/launch settings.

## 0.4.4 — refined WPF dashboard and resizable comparison (UI only)
- Approximates the approved design with a dark-blue Playnite-style header, toolbar icons, active/review/completed/ignored tabs, metadata image gallery and recent history.
- Adds draggable main and history splitters; both panel widths and the inner current/suggested comparison columns can be resized.
- Adds a separate resizable full metadata comparison dialog on button or double-click. All preview rows describe actual supported fields; other IGDB fields remain reference only.
- Preserves v0.4.3 matching, ignored decisions, guarded one-to-five-game writes, and separate undo journals.
- No unattended writes; no third-party graphics or framework dependencies.

## 0.4.3 — Playnite-style catalog dashboard (UI-focused)
- Responsive split-pane dashboard with Active/Completed/Review/Ignored/All tab navigation.
- Search and platform filters, selected-game metadata preview and cover lookup (IGDB public CDN).
- Persistent ignored-game IDs and dedicated restore action; no game records changed by ignore.
- Applied history from existing enrichment journal; completed entries remain accessible.
- Existing supervised five-game writer and undo remain unchanged. No unattended writes or additional metadata fields yet.

## 0.4.2 — supervised batch enrichment (development candidate)
- Adds first-click Mark checkboxes, Ctrl/Shift/Ctrl+A bulk marking controls and clear marks.
- Adds one confirmed, sequential batch of up to 5 Strong catalog identities; review/unmatched excluded.
- Re-fetches each exact IGDB identity, validates library state, fills only missing fields, retains same-platform name collision guards.
- Completed games hide from the attention view; partial success, cancellation, failure reasons and per-game durable undo remain.
- No unattended bulk writes, no overwrite of existing artwork, no new dependencies, no changes to ROMs or launch paths.

## 0.4.1 — coordinated one-game enrichment + Completed review (private dev)
- Added one-game, confirmed Playnite display-name update when collision-free; preserve custom sorting and ROM paths.
- Import missing IGDB cover, 90x90 thumbnail icon and landscape screenshot background alongside description. Preserve populated fields.
- Use a separate durable write-ahead enrichment journal with per-field undo, retaining original v0.4.0 metadata undo support.
- Add Needs attention / Completed / All results filter and persistent completed rows; immediately refresh after apply/undo.
- Add offline regression tests for collisions, Unicode, field-aware undo, interrupted transactions and corrupt journals.
- Retain 20/50/All catalog scanning; no automatic or unattended bulk metadata writes.
- Source-only/test validation is not proof of Windows compilation or live Playnite behavior.

## 0.4.0 — single-game metadata write and rollback (private dev preview)
- Add explicit single-row IGDB metadata confirmation and fetch-by-ID verification.
- Fill only missing description and cover; preserve all existing metadata, titles, ROM paths and launch actions.
- Import verified JPG cover into Playnite media database, with size/type restrictions.
- Add separate append-only metadata write-ahead journal and guarded last-operation undo.
- Keep candidate scans and CSV read-only; no unattended/bulk metadata modifications.
- Add offline metadata-plan and journal tests. Live Windows Playnite write/undo tests pending.

## 0.3.4 — Catalog scan batching and testing-client exclusions (read-only)
- Add 20 / 50 (default) / All remaining scan-size selector; continue where the current window left off.
- Exclude titles ending in Public Testing, Public Test, or Community Test Environment from catalog lookup, without changing Playnite games.
- Keep progress/cancellation, accumulated results and cumulative CSV export; restore controls after cancelled or failed runs.
- Preserve known-working direct /v4/games endpoint, IGDB pacing, matching rules, and explicit Playnite name rename/undo.
- Add offline regression tests for batch bounds and safe exclusion patterns. No automatic metadata writes.

## 0.3.3 — candidate identity and search normalization (read-only)
- Prefer plausible correct-platform candidates over exact wrong-platform candidates, with manual review unless the exact edition is independently corroborated.
- Allow unique exact-title + same-platform candidates despite nearby sequels; flag duplicate exact catalog entries.
- Canonicalize `and`/`&`, preserve sequel numbers, reduce short-title false reviews without ignoring generic unresolved names.
- Add bounded discovery-only catalog variants for Pokémon Yellow, King of Fighters EX, and GBA Mario Kart, with conservative review gates.
- Retain /v4/games, credential storage, existing Playnite rename/undo and the read-only catalog workflow.
- Regression tests extend the 20-game IGDB sample. No catalog fields are applied to games.

## 0.3.2 — Read-only IGDB transport regression fix
- Restored the formerly working IGDB `/v4/games` endpoint after v0.3.1's multiquery returned zero results for all 20 live cases.
- Retain bounded progressive search variants, platform-ID corroboration, and alternative-candidate ranking.
- Enforce query sanitization, deduplicate catalog IDs, and pace/retry requests.
- No metadata writing; Playnite title rename/undo continues unchanged.
- C# and live IGDB verification still required; do not treat the offline tests as an API integration test.

## 0.3.1 — private IGDB resolver improvement (read-only)
- Replace one literal query/game with one bounded multiquery (up to four search variants).
- Add a limited set of known catalog spellings, punctuation, and descriptive-title fallbacks;
  weak search variants alone cannot produce a Strong candidate classification.
- Match structured IGDB platform IDs when returned and deduplicate candidate IDs.
- Show up to three alternative candidates, attempted queries, candidate count and release year;
  export these as CSV for accuracy review.
- Use limited 429/transient retries and in-memory per-scan query caching.
- Existing explicit name rename/undo remains available. Catalog scanning does not write metadata.
- Automated tests are offline. Actual Windows compilation and IGDB response behavior require live testing.

## 0.3.0 (private catalog discovery preview)
- Added user-authenticated, read-only IGDB catalog discovery, candidate ranking and CSV report.
- Expanded the objective beyond name cleaning to catalog identity matching and ultimately
  automatic metadata and artwork retrieval for high-confidence games.
- OAuth app credentials encrypted under Windows CurrentUser DPAPI; no credentials in the package.
- Fixed external API endpoints, bounded 20-game scans, rate-limit spacing and cancellation.
- Catalog discovery does not modify Playnite records. Existing approved renaming remains intact.
- Live API integration and full metadata application deliberately deferred pending accuracy tests.

# Changelog

## 0.2.0 — private development checkpoint
- Add explicit name-only batch update after a scrollable full confirmation preview.
- Add private persistent write-ahead journal and latest-batch undo, with exact-name stale guards.
- Block title collisions, revalidate proposals, and preserve all unrelated Playnite fields.
- Correct dark-theme labels using Playnite TextBrush and improve platform visibility.
- Add journal round-trip/stacked-undo tests.
- No automatic background operation or metadata network integration.

## 0.1.2 — read-only metadata triage (private development)
- Display missing-core-metadata games even if no rename is suggested.
- Add metadata status/fields and missing-all/some/background-only filters.
- Add PSP-platform-aware suffix candidates and three conservative review aliases.
- Keep collisions on Review; mark/copy only true rename proposals.
- Add pure rule and metadata classification tests; no database mutation.

# Changelog

## 0.1.0-preview — private development checkpoint (2026-11-05)
- Created read-only Playnite GenericPlugin and WPF preview window.
- Added deterministic whitespace, two-digit collection prefix, trailing article rules.
- Added sequence-context and same-platform/source collision checks.
- Included search, filtering, first-click mark semantics, multi-selection, and copy-to-clipboard.
- Added source-only regression tests, build script and privacy-oriented packaging defaults.
- No Playnite database update, external catalog access or game-file modification yet.

- Allow one personally identity-verified Strong candidate through existing manual supervised path; never automatically treat a heuristic Strong label as verified.

## 0.4.12 — Consolidated UI preview (private)
- Scan and review actions are primary; scan size/title-rename options live under an expandable panel.
- Right-hand preview gains space, artwork size and sensible default selected results.
- No-import history collapses to a single informative line.
- Review plan supports checkbox marking directly, select all strict-ready, clearing marks and filtering by text without returning to the dashboard.
- Fix misleading preview count when IGDB returned no description.
- No change to catalog resolution, writer, journal, stored credentials or public GitHub.

## v0.4.15 — Dated history and UX checkpoint
- Preserve committed operations in history after undo, without re-enabling undo for those operations.
- Record UTC prepare/commit/undo dates on new journal operations, while legacy timestamps remain unknown; accept historical two-field markers.
- Add Date and visually distinct applied/undone statuses in Recent Updates / Full History; add selection count guidance.
- Distinguish review and unmatched states visually; rename history action to clarify Playnite-only title edits.
- Automatically guard build against mismatched YAML/source/DLL versions.
- No IGDB matching, ROM, Playnite metadata writer, or launch configuration changes.

## 0.4.16 — Title cleanup and reversible recent-history removal (private preview)
- User-approved Playnite-only title correction adds a platform suffix (e.g. `Super Mario Advance (GBA)`) when the clean catalog title already exists on the same platform. No ROM-file, emulator or install-path renaming.
- Mixed Applied/Undone selection no longer silently blocks all rename operations; only eligible distinct applied games are considered and skips are disclosed before approval.
- Recent Updates adds reversible **Remove from recent**; Full History offers **Show hidden** and **Restore selected**. Undo/write-ahead journal records are never deleted.
- Tests cover collision suffixes, unsafe second collisions, persistence and malformed visibility state.

## v0.4.17 — Integrated title cleanup during approved import
- Approving a verified IGDB identity now includes the normalized Playnite display-name update in the same journaled metadata operation. No separate history action required.
- When another game on the same platform already uses the clean title, propose the official title with a platform suffix (e.g., Super Mario Advance (GBA)). If that also conflicts, leave title untouched and report manual-review need.
- Preview, review plan and final confirmation display the same planned name; live writer fails closed if the approved title differs at commit time.
- Preserve launch actions, ROM files, existing metadata, journaling and undo; no unattended writes.

## v0.4.18 — Everyday selection and review UX
- Consolidate separate mark/unmark buttons into one contextual select/deselect action for highlighted rows. Add a matching toggle for currently visible ready games. Move Clear all and Ignore/restore to a More menu.
- Place the primary Review & apply action beside the results selection controls; show selected count. Preserve independent row highlighting versus import checkboxes.
- Simplify review dialog headings, selection labels, match status and approval action; preserve exact confirmation, test cap and recovery safeguards.
- Improve selected-row status contrast and use softer amber for review, muted coral for unmatched. Group uncommon history actions in a dropdown.
- No changes to IGDB matching logic, import writer, ROM paths, or journal schema.

## v0.4.19 — Direct actions and cautious undated IGDB matches
- Removed the main results More menu and Recent Updates History actions menu.
- Exposed direct, context-sensitive Ignore/Restore, Clear selection, Restore changes, and Remove from recent actions with clear titles and tooltips.
- Kept Full History as the place to restore hidden entries and inspect journal events.
- Promoted unique, exact structured-platform matches with substantive summary evidence when the release year is unavailable; known generic series and duplicate same-platform IDs remain review.
- Added Fire Dragon Game Boy positive/negative regression tests. ROM paths, metadata writer, journals, and approval boundaries unchanged.

## 0.4.22 — durable media staging detection (private preview)
- Append-only staged-artwork ledger begins before the first Playnite media side effect, tracks returned cover/icon/background media IDs, and transfers ownership only after the full enrichment undo record is durably prepared.
- Interrupted, unrecorded or partially cleaned media staging blocks later imports instead of silently continuing. A failed cleanup is never recorded as successful.
- Fail-closed recovery message provides the operation identifier. No automatic filesystem/ROM edits, unsafe guessed deletion, or unattended bulk writes.
- Added offline interruption/replay/corruption tests; original undo journal format remains unchanged.

## v0.4.24 — Recovery Consistency & Final Preflight (private preview)
- Read-only consistency audit cross-checks every completed artwork handoff against its metadata journal entry; blocked orphan/mismatched records prevent new writes. Legacy field-only journal records remain valid.
- Recovery Status shows unresolved and handed-off stages, clean abandons, pending operations and detailed reasons for blockers.
- Final approval displays *potential* field changes beside the proposed Playnite title; a live IGDB/Playnite preflight still decides actual writes.
- Added 17 recovery/preview regression checks, including persisted journal replay; preserved the supervised five-game limit pending live Playnite recovery testing.
