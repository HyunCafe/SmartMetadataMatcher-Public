# Design C v0.5.31 — Native saved-match refresh and evidence

## Why

Design C can now configure Twitch/IGDB credentials and scan new games. Previously, a user still had to switch to Classic merely to recheck a cached/suggested identity. v0.5.31 removes that piece of the navigation gap without adding a second importer or granting trust to a saved candidate.

## What changed

- **Refresh this IGDB match** appears in Game Details for actionable games with a saved result. It runs an explicit, single-game live IGDB search through the unchanged `WorkspaceCatalogLookup` and Classic matching resolver.
- **Refresh marked saved** in the toolbar refreshes up to 25 explicitly marked cached results, with progress and cancellation. This never scans unmarked games; mark and highlight remain independent.
- Before a refresh, live Playnite title, platform, supported-field gaps and cached identity inputs are checked. A stale or damaged cache stops the operation rather than being overwritten.
- A fresh result overwrites only the existing normal scan-cache record through its atomic checkpoint mechanism. If IGDB ID or title differs, that game's in-memory review mark is cleared; no approval or identity-confirmation is carried forward.
- Expand **Saved match evidence · not verified** in the inspector to see IGDB ID, cached score (explicitly not a calibrated probability), saved lookup time, and a bounded excerpt of the resolver's evidence. No passive network calls are made while selecting rows.
- Credential settings, first scans, selected-only Classic review, existing full history and rollback stay available.

## Explicit boundaries

- Fresh lookup **does not** establish manual identity confirmation, strict batch readiness or a live import plan. No Playnite metadata, game images, journals, approval states or credential files are written by the refresh operation; the ordinary scan cache is the only mutable output.
- The `CatalogCredentialStore`, `IgdbReadOnly`, `CatalogWindow.ResolveWithExactFallback`, `CatalogScanCache` and `CatalogScanCheckpoint` remain shared. No duplicate matcher or writer.
- The importer, actual field-level availability, manual wrong-match correction, final approval and undo still operate through Classic until each has an equivalent safe Design C controller.
- Empty/invalid evidence never retains an old mark on refresh; failed/cancelled operations are visibly reported and don't silently confer eligibility.

## Acceptance in Playnite 10

1. An already scanned game shows a saved IGDB ID/score/evidence after opening the evidence expander. Unscanned games cannot activate its refresh button.
2. Refresh one previously saved game. Observe progress and no Playnite game metadata changes. On an unchanged exact identity, marks may remain, but no approval is granted.
3. Mark 30 saved games and refresh; only the first 25 by displayed game-title ordering are included. The other marked games remain marked for a later batch. Pending/unmarked games are never refreshed.
4. Change a saved candidate's IGDB identity in a synthetic or controlled test and confirm its mark is removed on recheck. No wrong edition is silently approved.
5. Cancel midway and confirm only completed lookups are kept; action buttons recover correctly, and the cache remains readable.
6. Remove/disconnect credentials or network: show an error without clearing the cache or modifying Playnite metadata.
7. Test at 125–200% Windows scaling, narrow/large window, keyboard navigation and mark behavior, and check the Classic fallback.

## Feature-parity progress

Configure and scan are native (v0.5.30); **explicit refresh and inspector evidence** are native (v0.5.31). Identity correction, live field planning, draft editing, manual/strict approval, imports, history/undo, recovery and diagnostics remain gated before Classic is retired. No commits or public releases are part of this preview builder.
