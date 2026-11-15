# Smart Metadata Matcher 0.5.19 — Refresh required toolbar

This private preview adds a visible **Refresh required (N)** button next to Scan games.
The N value counts only previously saved Strong candidate results that require
live IGDB verification before strict metadata-review eligibility.

- One click collects all matching cached rows across the loaded library and
  calls the pre-existing `RefreshSelectedCatalogMatches` read-only worker.
- Confirm batches over 50 matches. Click Stop after current game to cancel.
- Freshly scanned results, completed/ignored entries, unmatched results,
  manual-review rows, and previously confirmed identities are excluded.
- No Playnite metadata writes, no replacement of existing journal entries,
  no changes to the pending ROM scan queue, no auto-approval.
- Normal Scan games, Review matches, and Tools > Rescan all remain intact.
- Known caveat: selected cached refreshes use the current worker's end-of-run
  save. A power interruption during a large refresh can lose that run's unsaved
  tail; run a moderate batch if reliability is critical.

### Validation
The ZIP app runs read-only source checks (--check-only), staged Windows Release
build and offline tests, and source/package audits BEFORE changing local source.
It backs up modified source files, then atomically replaces them.
It produces a private Playnite .pext for explicit installation and does not
write to the GitHub repository or automatically modify Playnite.
