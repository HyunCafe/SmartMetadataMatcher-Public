# Smart Metadata Matcher — v0.5.0 private RC2

**Scope:** personal-use candidate for Playnite 10, not a public release or security certification. This checkpoint freezes the v0.4.32 IGDB matching rules, import writer, title planner, undo journals and recovery format. Upgrade changes affect build validation, packaging, version labels and release documentation only.

## Verified from user live tests before RC1

- One supervised metadata import applied Playnite titles, text and media and produced a usable journal.
- An earlier one-game **Restore changes** test returned the original metadata.
- A two-game v0.4.32 batch applied 2/2 with zero reported failures and distinct history rows.
- Recovery Status after that batch showed six artwork handoffs, zero unresolved stages and no detected inconsistencies.

These observations do **not** prove all failure modes, all consoles, or uninterrupted operation at the full batch limit.

## Current operation

1. Run the guarded release-candidate upgrade from a **clean Git checkpoint**. Its Windows script compiles the full extension, executes offline tests, validates source/package integrity and creates a local Git commit.
2. The `.pext` contains only `SmartMetadataMatcher.dll` and `extension.yaml`. Store previous known-good packages and source backups for rollback.
3. Install the new RC in Playnite only with a restorable backup of the library, media, and extension data. Scan/cached results remain read-only until explicitly approved.
4. Strict import is bounded to **25 independently strict-ready** games per reviewed batch. Manually confirmed identities are bounded to 5, and non-ready exceptions remain single-game. No unattended writes.
5. The importer checks Playnite identity and platform, live catalog identity, proposed title, selection policy and recovery journal state before mutation. ROMs and emulator/launch paths are never changed.

## Release acceptance matrix

| Gate | Evidence | Status |
| --- | --- | --- |
| Windows full-extension compile | v0.5.0 user build output | Awaiting candidate build |
| Offline C# matching, drafts, batch and recovery tests | Windows CLI results | Awaiting candidate build |
| Fixed source/package inventory and privacy check | Verifier output; binary is not independently authenticated | Awaiting candidate build |
| Single-game metadata import + undo | User Playnite observation on prior version | Passed previously |
| Two-game import and recovery readout | User Playnite v0.4.32 screenshots | Passed previously |
| **independent undo** after multi-game import | Undo only latest operation; prior item remains applied | Not yet demonstrated |
| Intentional interrupted artwork crash | Disposable test library, journal inspection | Not yet demonstrated; do not test on primary library |
| Full 25-game batch | Controlled data set with explicit approval | Not yet demonstrated; do not require this for personal RC |
| Third-party dependency binary provenance | NuGet and PlayniteSDK assembly examination | Not independently verified |
| Public GitHub publication | Separate owner approval | Not authorized |

## Known limitations and technical debt

- `CatalogWindow.cs` combines UI, network orchestration and batch flow (over 3,000 lines). Refactor only with runtime coverage, not during RC freeze.
- IGDB Strong is a conservative *heuristic*, not a calibrated probability. Saved scans and user-confirmed identities cannot bypass live revalidation.
- Artwork added before its returned ID can be durably recorded remains a narrow interruption-recovery limit. The plugin fails closed and requires backup-backed recovery review; it must not guess which file to delete.
- The enrichment journal and the media staging ledger are not an atomic database transaction. Partial committed batches remain separately undoable, provided fields have not subsequently changed.
- Existing test-project `CS0649` warnings involve fields used by the full writer but not assigned in the reduced offline test assembly; warnings do not establish unused production fields.
- Fixed NuGet package versions are not by themselves verification of all transitive dependencies, their maintainers, or their compiled contents.

## Private packaging and privacy

Keep diagnostic exports and local scan data out of source control; `.gitignore` excludes private CSV/credentials/archives/IDE/build caches. The packager verifies the exact built DLL and extension manifest, screens for known private path and token patterns, and rejects unexpected ZIP members. This is a proportional screening step, **not** a complete secret scan or proof of compiled-binary security.

Never upload project files or publish to GitHub as part of local build/verification. Preserve pre-RC backup and Git commit for rollback.

## RC2 acceptance and boundaries
- Source check: all nine supported metadata fields participate in read-only discovery and cache invalidation; completed/ignored games remain excluded.
- Shared object mutation after staging Begin is treated as uncertain until journal preparation and handoff. Failed pre-journal stages block subsequent writes rather than silently claiming clean recovery.
- The newly expanded queue may contain more games than RC1. Its legacy cached results are read-only; live IGDB/platform verification and separate approval remain required.
- Live testing of a newly supported icon-only or genre-only game, and fault-injected reference-object Add failures in disposable Playnite data remain outstanding.
