# Design C v0.5.32 — Native IGDB identity review

## Behavior

A saved, unresolved game in the Design C inspector now provides **Find / correct IGDB match**, **Confirm saved identity** and **Forget confirmation** (only when applicable).

Find/Correct opens an owned dialog where the user can search IGDB by title or fetch an exact positive numeric IGDB ID. Results show name, ID, date, and up to three platform names. On selection, the plugin fetches the chosen ID again from the fixed official IGDB endpoint before asking whether to save that identity as a new **Review** suggestion. The old confirmation is revoked and the game's in-memory mark is removed; no automatic strong-match evidence or import approval is granted.

Confirm saved identity always refetches the exact ID and checks the current Playnite game title, platform, missing-field snapshot, and cache entry. A changed IGDB title blocks confirmation; the user must correct or refresh it first. Confirmation is a separate Yes/No user action, persisted through the existing `CatalogConfirmedIdentities` store. Forget confirmation explicitly revokes the same decision.

All credentials are loaded through the existing Windows DPAPI store. No credentials are written to logs, results, or package. Search operations are explicit and cancel when the dialog closes. The metadata writer and all journals are unchanged.

## Important limitations

This milestone does **not** provide live available-field planning, an import approval dialog, or direct Design C metadata writing. Do not retire Classic yet. A manually chosen game can be the wrong edition or platform; users must check the IGDB record themselves, and final Classic import preflight retains its original checks.

Correction is currently available for games that already have a cached lookup (including cached unmatched games). Initial scans are handled by Design C's existing Scan pending action.

## Runtime acceptance

1. Find a Review/Suggested game, search a partial title, inspect returned platform/year/ID, choose another version and reject the prompt; verify cache and marks unchanged.
2. Choose the exact alternate ID and accept. Verify the old mark clears and the new saved candidate is explicitly classified as needing review, never ready for automatic import.
3. Confirm the newly saved suggestion and verify the confirmation survives restart but does not import any metadata.
4. Revoke confirmation; verify it disappears without changing Playnite fields or undo history.
5. Change Playnite game name/platform/missing metadata before confirming; the operation must fail closed and ask for Reload.
6. Test credentials missing, wrong ID, IGDB error, cancel during network request, malformed scan-cache or confirmed-identity file, and an IGDB title change. Preserve prior valid data and block unsafe confirmation.
7. Test 125–200% DPI, keyboard, long platform labels and mixed PC/emulated games.
