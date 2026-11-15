# Design C — v0.5.50 Native Full Rescan and Archive

The Advanced menu adds **Rescan all IGDB matches (archive saved results)**. This is an opt-in, Yes/No-confirmed action. A valid local saved lookup cache is archived via the existing `CatalogScanCache.ArchiveAndClear`; no automatic IGDB request is started. User-selected marks, live-field proposals and unsaved draft choices are cleared because they are invalidated by a full rescan. Reopen **Scan all pending** to refresh the queue using existing 25-game checkpoints.

**Not modified:** Playnite games or ROMs, existing metadata, launch paths, user-confirmed identity store, ignored choices, history, enrichment and media journals, conditional undo, Classic fallback, matcher, importer, and credentials.

**Safety:** Reject busy operations, fail before moving a malformed cache, require visible consent, and preserve an archived file rather than silently discarding saved lookup data.

**Verification:** Source checks and full staged regression scripts run before local isolated Windows build; a Playnite UI smoke test is still required. This feature does not grant strict automatic-import trust. Future release: fresh strict evidence requires capturing resolver output at scan time, not reusing stale Strong labels.
