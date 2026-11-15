# SmartMetadataMatcher v0.5.51 — Fresh strict evidence in Design C

Design C's Advanced menu gains **Verify fresh strict readiness for marked games (read-only)**. This reruns the original bounded IGDB candidate searches and rival comparison for each selected saved, unconfirmed candidate. It calls the original `CatalogCore.CanAutoPlan(CatalogMatch)` gate and accepts proof only if the live result is exactly the stored IGDB ID/title and the current Playnite title, platform, missing fields and release year remain unchanged. All network work is cancellable. No Playnite metadata, identity-confirmation store, scan cache or journal writes occur.

Evidence is kept **only in the current window's memory** and invalidated by library changes, saved scan refresh, user confirmation and full-rescan archive. A persisted Strong label, similarity score, or exact-ID field fetch alone is never treated as fresh strict proof. The existing pure batch planner now receives the typed proof and distinguishes `Fresh strict evidence` from human-confirmed and unverified candidates. Strict eligibility is only permission to continue read-only preflight—not approval to import. The existing native writer still requires explicit per-game confirmation; the Classic fallback remains for strict automatic batch execution while its separate authorization path is migrated.

## Validation

Run the package's `--self-test`, all inherited Python source regression scripts, Windows .NET Release build and C# tests, and `.pext` verification before applying. Live Playnite/IGDB network and import/undo tests remain unverified until performed locally. Test stale cached labels, changed platform/title, edits during network fetch, partial cancellation, identity correction and recovery blockers. Do not retire Classic yet.

## Provider isolation fix

The IGDB network client and original bounded resolver now run in `WorkspaceFreshStrictLookup`; Design C only orchestrates the read-only operation and publishes session-local evidence after current Playnite/cache revalidation. Existing UI no-provider/no-writer regression tests remain unchanged.
