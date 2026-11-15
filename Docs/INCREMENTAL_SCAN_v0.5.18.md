# Incremental IGDB scanning — v0.5.18 preview

## Behavior

- Startup rebuilds the catalog eligibility list from the current Playnite library.
- Previously completed, ignored, or unchanged games with saved scan results do not trigger another default IGDB query.
- The read-only scan cache has no seven-day expiry; unchanged records persist between launches and new ROM imports.
- Cached records are compared against Playnite game ID, exact title, platform, local release year, and missing metadata-field mask/count. A relevant change queues a fresh lookup.
- Cached candidates are **read-only**: they cannot become automatically approved metadata writes. Fresh IGDB validation and user approval are still required when applying edits.
- **Tools > Rescan all** is intentionally destructive to the read-only cache only: it archives the old scan snapshot and queues unresolved games afresh. Use it only when you want a full refresh, not to discover newly imported games.
- **Refresh selected saved matches** is available for targeted revalidation.
- No Playnite database, ROM, save, journal, or credential changes are performed by scanning or by this source updater.

## Limits

- Only complete checkpointed results are reusable. Interrupted scans may need to retry the final uncheckpointed batch (up to nine records).
- A cache that is missing, corrupted, manually archived, cleared or relocated cannot be reused.
- Malformed or absent scan timestamps remain invalid and must not become immortal cache hits.
- Legacy records without saved year evidence require one new lookup for games with a known Playnite release year; undated games retain their old records.
- The cache remains bounded to 10,000 records / 8 MiB and requires attention if these limits are reached; indefinite freshness does not mean unbounded storage.
- A manually forced full rescan still queries every unresolved eligible game; this is intentional.
- Runtime behavior with the user's exact private build and Playnite host must still be confirmed before release.

## Quick test

1. Open Matcher; read **awaiting IGDB lookup** and **unchanged saved results** independently.
2. Scan new/unscanned entries, close Matcher, reopen. The unchanged entries should now be in the saved count, not the pending lookup count.
3. Import one new PSP/PS1 ROM into Playnite, reopen Matcher. Only new or changed eligible games should be newly queued.
4. Verify that Review / Apply still performs live validation of saved candidate identities.
5. Confirm ignored and completed history is preserved. Do not use **Rescan all** during steps 1–4.
