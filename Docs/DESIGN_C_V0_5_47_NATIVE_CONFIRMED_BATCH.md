# v0.5.47 — Native Confirmed Batch Import

Design C can import a batch of two or more **individually user-confirmed** IGDB identities after one itemized final review. Existing single-game native import and Classic fallback remain available.

## Execution policy

- Every selected game must have a previously confirmed matching exact IGDB ID/title/platform. Cached Strong or similar scores are not automatic authorization.
- An exact live field check must complete for all selected games before the native batch approval button enables.
- Freeze game/metadata snapshots, planned Playnite titles and staged drafts. Validate full selection before approval and again after the final approval modal.
- Require an explicit restorable backup acknowledgement, show per-game field and proposed title changes, and identify that the batch is **not atomic**.
- Execute sequentially through `WorkspaceNativeSingleImport` -> `WorkspaceImportCoordinator` -> `CatalogEnrichmentWriter` on the Playnite dispatcher. Re-fetch IGDB identity and revalidate confirmed identities and Playnite data for each game. Never modify ROMs, file paths, executables, game IDs or launch actions.
- Stop on the first error or cancellation. Committed games remain in the original journal with their independent undo. Display applied/skipped/failed/not-attempted counts; no unattended retry or guessed recovery.
- Preserve the existing Classic advanced/import path for unconfirmed or more complicated selections until the native interface reaches parity.

## Verification limitations

The Python source regression checks and isolated Windows Release build/tests detect many regressions but **do not constitute live Playnite multi-game import, media staging, undo or crash recovery testing**. Before real metadata writes, use a disposable Playnite library with a restorable copy of Playnite and extension data. Test a small confirmed batch first; inspect the History/Undo and Recovery flows. Large-batch production use is not yet certified.

## Next

Finish Design C parity for strict-ready batch approval (without requiring individual confirmation of every strict-ready identity), integrated history/undo/recovery and maintenance tools; archive Classic only after acceptance tests.
