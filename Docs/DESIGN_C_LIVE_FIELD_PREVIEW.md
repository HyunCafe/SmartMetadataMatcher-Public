# v0.5.35 — Exact IGDB field-availability comparison inside Design C

## Behavior

- Select a game with a saved IGDB identity in Design C.
- In **Game Details > Metadata comparison**, choose **Check IGDB field availability**.
- A bounded **single-ID** IGDB fetch reads complete metadata fields through the existing `WorkspaceIdentityReview.FetchExact` implementation.
- The comparison presents missing Playnite fields and values available on that exact IGDB record: description, cover, background, icon, platform-specific release year, genres, developers, publishers, and series. Existing cover and display title remain explicitly protected.
- Up to one focused game's checked fields are held **in memory only**. Reloading the library clears this comparison.
- If the saved candidate ID/title, game name/platform/missing field mask, or original lookup cache changes during the fetch, results are discarded. An invalid or expired saved lookup fails closed.
- Requests can be canceled. UI controls prevent overlapping field fetches with library scans. No silent network lookup occurs on selection.

## Boundaries

This is **not a write plan or import authorization**. The candidate might still be the wrong game/edition. Media image IDs do not certify a downloadable JPEG; the existing importer still checks downloads. Playnite shared metadata objects are not created. The display title stays unchanged until a separately approved rename. Completion, backups, journals, and undo remain the responsibility of the original supervised implementation.

## Test plan

1. Confirm `--check-only` makes no changes. Run the guarded builder on exact v0.5.34; require Windows Release build, all offline C# and source tests, and package verification before applying.
2. Choose a saved identity for a game missing Series and Cover. Check available fields. Verify that Series reports IGDB collection names only if supplied, Cover reports image availability only (not imported), and title remains unchanged.
3. Test a GBA game whose IGDB record contains a release year on another platform; the preview must not display that as the GBA release year.
4. Navigate away and back, then reload (F5), then change suggested IGDB identity. Verify previews never apply to a different game/ID or persist across reload.
5. Test empty credentials, HTTP failure, cancellation, damaged scan-cache file and live Playnite title/platform changes. No metadata should be modified. Test existing Classic review/import fallback and undo regression.

## Next parity milestones

- Editable import draft and per-field opt-in/opt-out with preserved metadata defaults.
- Full selected batch exact field previews, approval, backup acknowledgment and safe application in Design C through the existing writer; no second writer.
- Ignore/restore, history undo, recovery, diagnostics, title cleanup, and complete keyboard/accessibility parity before Classic retirement.
