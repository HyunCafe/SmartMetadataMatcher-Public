# SmartMetadataMatcher — v0.5.37 consolidated parity audit and next-session handoff

**Target:** One Design C workspace, not two competing UI workflows. Preserve Playnite 10, PC/emulator matching, existing updates, saved identity decisions, ignored games, history/undo, journals, media staging, and recovery. No public commit/push during development. This handoff is project documentation, NOT universal memory.

## Source baseline and known evidence

- Last user-confirmed Windows compilation before this work: **v0.5.35**. The v0.5.36 **repaired** builder was handed over, but its Windows build result has **not been reported in this conversation**. v0.5.37 must verify the resulting local v0.5.36 source before applying.
- The exact v0.5.36 workspace source is packaged in `SmartMetadataMatcher-DesignC-v0.5.36-NativeDrafts-Builder-Fixed.zip`, SHA-256 of `Source/LibraryWorkspacePreview.cs`: `b9b9dcd871d829acf1cfd57fdcfac53c2f8b4431d58b1cf1914c6c2af43d8394`.
- Public repository main is older than local changes. Never overwrite local development files with GitHub main.
- Existing selected-only handoff contains **`useCatalogTitle.IsChecked=false;`**, which silently disables title cleanup. Classic normally initializes **`Clean titles on import`** checked. This is a genuine workflow inconsistency, not a wording issue.
- v0.5.36 drafts are in memory, scoped to game, exact IGDB ID/title, metadata missing-mask, and release-year snapshot. They do **not** transfer into Classic or invoke an importer. Native field inspection is also read-only.

## Product default: title cleanup

Treat Playnite display title cleanup as the standard on approved import. Do not expose a separate `Clean titles on import` toggle. Never rename ROM files, paths, executables, launch actions or Playnite game IDs. Always show the proposed display-name change in the final review. Preserve edition/platform distinctions, allow validated custom title overrides, retain existing name where a collision cannot be resolved safely, and block invalid custom titles.

**v0.5.37 behavior:** Hide the Classic title cleanup checkbox from normal layout, keep its checked state enabled for all imports, remove the selected-only handoff exception that disabled it. Use the existing `EnrichmentPlan.SuggestedUniqueDisplayTitle` / `CatalogImportDraft.ResolveTitle` planners, not a new name-normalization engine. Keep final approval and backup checks unchanged.

## Feature-parity audit — no silent omissions

| Capability | Priority | Current status / remaining gate |
|---|---|---|
| Single Design C entry point, minimize Classic exposure | P0 | Dual UI still available; do not retire Classic before full parity |
| Secure Twitch ID/secret entry, DPAPI persistence and connection test | P0 | Native since v0.5.30; test first-run/error paths |
| Error feedback for OAuth failures, rate limits, HTTP 504 and cancellation | P0 | Shared IGDB calls exist; UI feedback/runtime testing required |
| Incremental new-game scanning, 25-game batches and saved lookup reuse | P0 | Native scan exists; 7-day cache retention may conflict with desired unchanged-game reuse |
| Scoped cached-result refresh; single result; marked batch | P0 | Native since v0.5.31; preserve exact ID/title invalidation |
| Full rescan with explicit cache archive/confirmation | P1 | Classic only; must not silently discard cached choices |
| Exclude demos/playtests/SDK, strict PC/emulator platform and edition checks | P0 | Reuse existing resolver, inspect existing tests and edge cases before retiring Classic |
| Manual IGDB search, alternative results, exact-ID verification and correction | P0 | Native since v0.5.32; verify platform mismatch presentation and selection safety |
| Human-confirmed identity; forget/invalidation | P0 | Native; distinct from write approval |
| Read-only field availability with platform-specific release date | P0 | Native since v0.5.35; do not infer import readiness from an IGDB field hint |
| Field-by-field proposed changes and keep-existing semantics | P0 | Native comparison/draft; exact final write plan still missing |
| Draft opt-outs, custom display title and custom description | P0 | v0.5.36 session-only; **NOT YET passed to the importer** |
| Default automatic cleanup of Playnite display title; no toggle | P0 | Addressed in v0.5.37 preview/fallback policy; final approval must show exact title |
| Title suffixes and collision checks across the library, not just platform | P0 | Existing `EnrichmentPlan`/`CatalogImportDraft`; fail closed on conflicting custom title |
| Mark vs highlight distinction, selection count/hidden marks, Ctrl/Shift | P1 | Native; keep intact |
| Native batch plan showing marked-only scope, identity readiness and proposed titles | P0 | **v0.5.37 read-only preview**; not yet a write plan |
| Strict-ready vs manually confirmed vs saved candidate; batch caps | P0 | Classic still authoritative; never promote saved scores to strict evidence |
| Full metadata plan, exact current Playnite data, field-draft mapping | P0 | Must extract shared coordinator; do not clone the writer or rely on UI-only preview |
| Final explicit approved-game list, fields, title, backup acknowledgment | P0 | Classic only; restore exact approval semantics in Design C |
| Same journaled sequential writer, image download validation, cleanup/rollback | P0 | Classic only; **must reuse existing implementation** |
| Failure during media staging, cross-ledger recovery and fail-closed gate | P0 | Existing services; wire to native UI before any write |
| Progress, partial success, stop at safe boundary, cancellation and retry | P0 | Native scan yes; native apply no |
| Persistent update history including applied/undone/hidden entries | P1 | Design C read-only view; advanced filters/hidden history controls incomplete |
| Undo most recent and selected-prefix stacked undo | P0 | Classic only; preserve journal ordering and safety |
| Ignore/restore game and saved decisions | P1 | Classic only, separate from dismissing a suggestion |
| Recovery status and corrective options | P0 | Classic only; must remain accessible when write gate is blocked |
| Export CSV and diagnostics, technical matching columns | P1 | Classic Tools/advanced only |
| Standalone title-only cleanup/rename | P2 | Classic Tools; retain access as advanced utility (distinct from import-title default) |
| Edit existing Playnite title/metadata and compare-context actions | P1 | Classic context menus; decide scope and require explicit approval |
| Search, platform and missing-field filters; pagination/large libraries | P1 | Native; runtime behavior at 1k+ games/DPI to test |
| Safe settings, selection state and existing data after restart | P0 | Most selection/drafts transient; never imply persistence where none exists |
| Playnite SDK compatibility, packaging privacy, secrets and no extra permissions | P0 | Windows Playnite 10 runtime verification + package audit required |

## v0.5.37 — deliverable and limits

1. Remove the user-facing import-title-cleanup checkbox; enforce checked true for Classic and the selected-only fallback.
2. Preview selected games through the existing pure `EnrichmentJobPlanner` with `requestRename=true`; show candidate/confirmation state, proposed display names, collision warnings, draft presence and required next step in Design C.
3. **No trust escalation:** saved scans cannot be promoted to strict verified identities from a score or title. Manual confirmation only authorizes another exact-ID check, not metadata writing.
4. Draft summary in Design C explicitly warns that v0.5.36 session drafts are **not** yet transferred to Classic. Do not falsely claim draft preferences apply to imports.
5. No direct import, changes to journal/writer, ROM files or Playnite metadata. The next milestone remains an extracted shared coordinator that maps native reviewed drafts to the proven approval/writer path.

## Next implementation order (not individual cosmetic versions)

**Milestone A — Shared coordinator:** Extract Classic's live preflight, policy, draft binding and approval-plan preparation behind an interface independent of `CatalogWindow` controls. Keep all current fail-closed tests and batch limits.

**Milestone B — Single-workspace approval and application:** Build one native final review with actual exact-ID/field/media/collision evidence and backup acknowledgment. Call the same sequential writer/recovery journal, not a second importer. After first verified update, prove undo.

**Milestone C — Remaining parity and retirement:** Native ignore/restore, recent/full/hidden history, recovery controls, exact rescan behavior, full diagnostics, title-only utility, keyboard/125–200% DPI verification. Side-by-side behavior tests on PC/PS1/PSP/GBA and ambiguous editions. Then remove Classic from normal navigation and consolidate unpublished Git changes into meaningful commits.

## Risk register and verification gates

- **P0:** Checked-only identity and import consent must remain distinct. An input selection, draft save or identity confirmation must never approve a write.
- **P0:** Handoff previously disabled title cleanup. Inspect exact local `CatalogWindow.cs` before building; verify default on in *all* paths and view proposed title at final approval.
- **P0:** Never let an older `catalog-scans-v1.json` saved result count as fresh strict evidence. Recheck current name/platform/edition and exact ID prior to writes.
- **P0:** v0.5.36 source must be present before applying v0.5.37. Do not assume successful download equals successful Windows compilation.
- **P0:** Test with two games sharing canonical names across consoles; ensure collision-safe suffix or retention, not silent wrong title.
- **P1:** Cached results have `TimeSpan.FromDays(7)` in the public source. Verify actual local v0.5.36 behavior and decide cache lifetime separately; don't delete old cache in a title/preview update.
- **P1:** Preserve 364+ existing committed updates and all undo/recovery journals; backup source is not a Playnite database backup.
- **P1:** Existing synthetic fixture tests previously had wrong hashes and stale text assertions. Check actual source anchors in Windows and run real .NET build/C# tests before applying any patch.

## Manual runtime acceptance (after build)

- Open Design C, mark one confirmed match and one cached/unconfirmed match. The batch preview must distinguish "eligible for live preflight" from "refresh required".
- Check a numbered ROM title; see planned cleaned Playnite display name. Mark an intentionally colliding title; see a collision warning or preserved original name, not a destructive overwrite.
- Open Classic normally and via selected review; neither path shows a "Clean titles on import" toggle. All actual proposed title changes still appear in final approval.
- Save a Design C custom draft. The batch preview must say the session draft does not yet carry over to Classic. It must not silently apply on approval.
- Test refresh cancellation, old confirmation, wrong IGDB edition and undo/recovery without writing any metadata in the preview step.

**Do not commit or push until full functionality and Playnite acceptance are verified.**
