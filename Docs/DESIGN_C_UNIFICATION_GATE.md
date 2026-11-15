# SmartMetadataMatcher — Single-workspace feature-parity gate

**Policy:** Design C is the primary target UI. Classic remains an internal safety fallback until all functional gates below pass. We do not ship a second matching algorithm, a second writer, or weaker approval criteria. Do not remove Classic simply because the new layout looks finished.

## Functional inventory and migration decisions

| Capability in Classic | Design C end-state | Gate / current transition | Risk if forgotten |
|---|---|---|---|
| Twitch Developer Client ID + Client Secret | **Settings** panel/dialog within Design C; secure password input; encrypted DPAPI storage; never expose or export secrets | v0.5.30 adds settings using the existing `CatalogCredentialStore` | Can't connect to IGDB |
| Test Twitch OAuth + IGDB connection, user-friendly auth/network/rate-limit errors | **Test connection** in Settings; cancellable and non-persistent | v0.5.30 uses the existing `IgdbReadOnly.TestConnection` | Blind scans and impossible troubleshooting |
| First-run setup / no credentials / malformed secure settings | Clear in-workspace setup/recovery prompt, without deleting invalid credentials | Settings in v0.5.30; first-run journey to validate | Cryptic startup failure |
| Read all Playnite platforms, originals and emulator titles | Library tab/filter and game inspector | Existing Design C | Missed ROM/PC titles |
| New-game discovery and incremental queue | **Scan pending**, skip unchanged supported-field identities, preserve cache across sessions | v0.5.30 native bounded scan (25 at a time) | Repeated 500-game rescans |
| Explicit **Rescan all**, selective refresh and saved-match refresh | Separate actions, require explanation/confirmation; never silently archive cache | v0.5.31 adds explicit single/marked saved-result refresh (up to 25); full rescan/clear still pending | Lost cache/IGDB throttling |
| IGDB querying, platform mapping, title normalization, aliases, roman numerals, editions, rival ranking | Shared resolver service; one algorithm for both UIs | v0.5.30 reuses `CatalogWindow.ResolveWithExactFallback` by exposing its existing implementation unchanged; later extract to backend class | Identity false positives |
| Reject playtest/demo/SDK/unrelated versions and ambiguous matches for automation | Preserve all current eligibility and diagnostics | Existing engine remains authoritative; parity pending | Wrong game updated |
| Suggested identity, alternatives, evidence, warnings, wrong-match correction | Inspector + alternative-match/search dialog, with explicit manual ID confirmation | v0.5.32 adds manual title/ID search, alternative choice, exact-ID re-fetch, and explicit confirm/forget in Design C; field planning remains pending | Users cannot resolve errors |
| Actual field availability, what will change, original-vs-proposed comparison | Live read-only IGDB fetch and field-by-field before/after plan | Existing inspector only shows possible changes; direct plan pending | Implied field improvement without evidence |
| Existing user metadata protection, rename/description exceptions, collisions | Field-specific draft controls with "keep existing" default | Pending direct editing | Destructive overwrite |
| Mark/highlight/filters, multi-select and persisted marks across transitions | Existing table/selection tray; never treat mark as import approval | Existing Design C | Wrong batch scope |
| Strict verified batch, manual-confirmed batches, supervised limits | Shared planner and policy in Design C, not copied UI implementations | Classic still owns execution; pending integration | Bypassing limits |
| Exact live ID/name/platform recheck immediately before write | Required in reusable backend import controller | Existing Classic enforcement; pending direct invocation | Stale identity write |
| Review selected IDs only; pending vs cached preparation, cancellation, retry | Inline preparation with progress and safe resume | v0.5.28 handoff; v0.5.30 native initial scan; v0.5.31 native selected saved-match refresh | Excess requests and lost selections |
| Final approval list, exact field preview, backup acknowledgement and blocked-destination warning | Single Design C approval dialog; no automatic import from scan or marking | Pending; Classic only | Unapproved metadata changes |
| Journaled sequential metadata writes; image staging/download; partial failure recovery; undo | Same existing writer/journals, invoked by shared controller | **Never rewrite/duplicate writer** | Corrupt Playnite library |
| Apply progress, cancel only at safe boundaries, proper error reporting | Workspace progress area and task results | Native scan v0.5.30; import pending | Uncertain partial operations |
| History browse/search/filter, record visibility, undo/restore and recovery status | Integrated History workspace/dialog with validated state and recovery actions | Design C has read-only history; undo/recovery pending | History and recovery stranded in Classic |
| Ignore / restore game, confirmed identity persistence/invalidation | Inspector controls and single source of truth in existing stores | v0.5.32 native confirm/forget, correction invalidation; ignore/restore still in Classic | Lost user decisions |
| Diagnostic CSV, support export, read-only logs, package/security diagnostics | Advanced Tools menu in Design C | Pending | Unreachable maintenance |
| Scan and cache statistics, clear network/runtime messages | Compact status area, counts, pending state, warning states | Native scan status v0.5.30 | Opaque operations |
| Keyboard shortcuts, Ctrl/Shift select, row menus, filters, resizing, accessibility and no clipped platform names | Current workspace behavior and regression tests | Existing Design C, verify across DPI | Usability regression |
| Defaults, user preferences, display-title options | In-workspace Settings / Advanced; preserve previous defaults | Pending | Silent behavioral change |
| Version numbers, migration compatibility, Playnite restart and UI modal/window lifetime | One stable entry point after validated parity | Temporary dual entries retained for rollback | Startup or workflow breakage |

## Release gates before retiring Classic

1. No feature in the inventory is marked Pending; evidence includes the actual implementation, not merely a button.
2. The **same** matching resolver, ID-verification policy, field-plan code, batch policy, metadata journal, recovery gate and undo implementation are called by the new UI. No duplicate importer.
3. Saved scans, user-confirmed identities, ignored games, existing 364+ update history, and undo records are never overwritten during migration. Test malformed or missing files fail closed.
4. Offline matching/policy/security tests, Windows Release build, and package integrity pass. Repeat runtime tests in Playnite 10 with PC and emulated games; include cancellation, network timeouts and 4K/125–200% DPI.
5. Run side-by-side read-only differential comparisons against Classic on representative games: strong, ambiguous, cached, changed, ignored, completed, artwork-only and optional Series-only gaps.
6. Demonstrate end-to-end **Configure → Scan → Review → Verify → Preview → Approve → Apply → History/Undo** without opening Classic or silently escalating trust.
7. Only then make Design C the default/only user-facing window; keep an internal fallback for a controlled transition and prepare a carefully scoped public commit/release.

## v0.5.30 scope and limits

- Direct Design C **IGDB/Twitch settings** with masked secret, same DPAPI store, asynchronous connection test.
- Direct Design C **first-lookup scanning** of up to 25 pending (or marked pending) games. Uses the original exact/rival resolver and atomic catalog scan cache, with cancel/progress. Scanning writes **only** local lookup-cache data, never Playnite metadata.
- No new automatic matching approval, live field availability inference, changes to existing import-journal/recovery code, or removal of Classic.
- Saved-match refresh, alternative matching, direct approval/import and undo remain deliberately unavailable inside Design C until their shared backend controllers are introduced and independently validated.

## v0.5.31 — live saved-result refresh and evidence

- A single saved result or 25 marked saved results can be explicitly refreshed within Design C, with progress/cancel and exact Playnite source checks. The original resolver and cache format remain authoritative.
- Changed IGDB ID/title removes the affected in-memory mark; refresh never confirms identities or approves imports.
- The inspector displays bounded cached resolver evidence, IGDB ID, score (not probability), and scan time; no provider calls on row selection.
- Full rescan, alternative choice, field-specific import preview and editing, final approval/import, recovery and undo **remain gated** before Classic can be retired.

## v0.5.32 — native candidate search, correction and explicit confirmation

- Manual title search returns at most 25 IGDB game records; a positive numeric ID can also be fetched exactly. No lookup occurs merely by opening the inspector or choosing a row.
- Selecting a proposed candidate **re-fetches the exact ID** and requires explicit Yes/No approval to stage it as a **Review-only saved scan**. Any prior confirmed-identity decision for the Playnite game is removed before the cache is replaced, and marked selection is cleared. No ranking score or strict identity evidence is invented.
- **Confirm saved identity** separately re-fetches exact IGDB ID/title, rechecks current Playnite title/platform/missing metadata and the current cache snapshot, and asks the user to verify platform, game, and edition. This only saves a decision in the established confirmed-identity store.
- **Forget confirmation** is separately available with a confirmation prompt; it leaves existing metadata, history, and undo untouched. All provider and store calls occur in a native service, not the WPF UI or a duplicated matching engine.
- Live import preflight, field-level plan/edits, final batch approval and execution, history undo, ignored-game restore, and recovery remain in Classic until separately migrated and verified.

## v0.5.33 — permanent interface locations before further migration

- Header utilities now hold **IGDB settings** (DPAPI Client ID/Secret and Test Connection), History and a visually secondary Classic fallback. Settings do not crowd the game search row.
- Bounded native scanning, marked saved-result refresh and cancellation share one library-action group and a compact status line. Existing category tabs, search and filters remain stable.
- IGDB candidate correction uses a structured, resizable table with title/platform/year/exact-ID/relevance hint. A **presentation-only** title/platform ordering makes closely related games easier to spot and unrelated fuzzy matches less prominent. This does not elevate trust, invent eligibility, bypass exact-ID fetch, or alter confirmation and import safety requirements.
- The inspector continues to contain identity decisions, evidence, potential field changes, and artwork. Pending field drafts, live comparison, approval, imported-write progress, undo, ignore/restore and diagnostics remain explicit parity gates. Do not retire Classic yet.


## v0.5.35: native exact field-availability check

Completed: explicit exact-ID live field comparison in Design C; missing-only candidate fields, platform-specific release date and in-memory-only display. This is not a writable import plan. Still required: editable field draft, selected batch approval, existing writer integration, history/undo, recovery, diagnostics, ignore/restore and complete parity testing.


## v0.5.36: native session draft editor

Completed: explicit per-game temporary field inclusion/exclusion, proposed title/description validated against existing CatalogImportDraft rules, collision protection, exact identity and Playnite snapshot revalidation. Draft is not transferred to Classic and authorizes no writes. Still required: connect drafts to the existing supervised review/approval path, safe import, undo/recovery, diagnostics and full feature-parity acceptance.


## v0.5.37: consolidated batch and default title parity

Playnite display-title cleanup is standard on approved import (checkbox removed, including selected-only review). Design C now previews marked identities, proposed titles, collision warnings and session draft presence through the existing pure job planner. This is not an approval or import plan. See Docs/DESIGN_C_PARITY_AUDIT_AND_HANDOFF.md for the complete feature inventory, risks and next-session checkpoints.
