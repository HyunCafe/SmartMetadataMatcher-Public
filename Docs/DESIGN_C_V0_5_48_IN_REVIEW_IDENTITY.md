# SmartMetadataMatcher v0.5.48 — Native marked-batch identity review

## Development step
Design C now offers **Review unconfirmed identities...** within the marked-game review dialog. The user can review each selected saved suggestion against a fresh exact IGDB record and explicitly confirm its Playnite game, platform and edition. The existing `WorkspaceIdentityReview.Confirm` and `CatalogConfirmedIdentities` store retain those decisions; an approval of one identity never approves an import.

After any identity review begins, all previous exact-field comparisons and import controls are invalidated. The user must rerun **Check exact fields**, inspect the field-by-field final plan, acknowledge a restorable Playnite and extension-data backup, and explicitly approve the same native single/confirmed-batch writer route. A decline pauses the sequence, preserving only decisions already confirmed. No limits are silently expanded, no metadata, ROM files or paths are edited by identity confirmation.

### Safeguards
- Every identity is fetched live by exact IGDB ID off the UI thread.
- Game ID, Playnite title/platform/missing-state, cache identity, exact IGDB title and structured platform are verified again on the UI thread before persisting an identity.
- Separate Yes/No prompt for each identity; no auto-confirm, no promotion of cached Strong scores to strict trust.
- Cancellation, missing/changed records and wrong-platform/edition results stop the sequence.
- Native imports still go through `WorkspaceNativeSingleImport` → `WorkspaceImportCoordinator` → `CatalogEnrichmentWriter`, with existing journals/undo; Classic remains a temporary fallback.

### Verification required
Builder self-test; all inherited Design C tests; Windows Release build and C# tests; Playnite launch. Test at least two unconfirmed games in a disposable backed-up Playnite library: choose No on one and confirm the other; confirm neither decision changed metadata; then verify the fresh field-check requirement, a two-game approved native import and journaled undo. A **successful compilation or launch does not certify import/undo**.

### Subsequent parity tasks
Fresh strict-ready matching evidence, native advanced undo/history/recovery tools, full rescans, scan-all-pending QoL, then retire Classic once all gates pass. Never infer new strict evidence from saved scores.
