# v0.5.1 Private Batch-Unlock Acceptance (CURRENT)

**Status: development candidate; Windows build and live 26+ item acceptance pending.**

- Unlimited (no fixed numeric cap) **unrestricted strict-ready selection** is enabled only for freshly verified strong evidence; cached/review/user-confirmed identities do not obtain strict authorization.
- The supervised path remains limited to five, and manually inspected non-ready exceptions remain single-item. All writes require a backup acknowledgement and separate final confirmation.
- Existing sequential writer, live recheck, first-error stop, cross-ledger recovery gate, per-game undo, journal/ROM isolation and cancellation stay unchanged.
- Pending: Windows compile/offline regression results; live multi-game performance beyond 25; injected crash recovery and independent dependency-binary certification. Earlier individual import and **independent undo** are user-reported passes.
- Provenance: preserve v0.5.0 local stable tag; do not publish or replace the private-stable package.

---

## Private RC3 — Precision and export audit (2026-11-06)

- Corrected false edition warning for ordinary titles such as *Hogwarts Legacy*. Explicit `- Legacy` and `Legacy Mode` remain review-sensitive.
- Exact alias matches with a material remaster/edition/client difference now require identity review (e.g., *The Falconeer* and *Realm of the Mad God Exalt*); matched edition titles are unaffected.
- Rejects implausibly low-similarity candidate recommendations such as *THE FINALS* → *Final Fantasy IV*, retaining manual search; adds search-only article-free and collection punctuation variants.
- Candidate CSV now includes nine-field Playnite gaps at export time, count, potential fills supported by scanned artwork/description evidence, and cached-result status. Scan evidence does not guarantee a write or validate the match.
- No ROM, emulator, import writer, journal schema, five/25 batch-policy or Playnite library changes during build.
- Existing RC2 checkpoint `91148ae` remains the rollback target. Windows compilation and live IGDB search remain unverified until performed locally.

# Smart Metadata Matcher — v0.5.0 private RC3

**Scope:** personal-use candidate for Playnite 10, not a public release or security certification. RC3 specifically refines read-only matching precision, adds search-only fallbacks and enhances the candidate CSV export. The import writer, title planner, undo journals, recovery format and batch policy remain unchanged.

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

## RC3 acceptance and boundaries
- Verify exact Hogwarts Legacy identity is not downgraded because its title contains an ordinary word.
- Verify catalog remaster/client mismatch triggers Review even if the provider supplies an exact alias (Falconeer, Realm of the Mad God Exalt).
- Verify unrelated fuzzy results are not displayed as recommended candidates (THE FINALS), while article-free and collection-format queries are search-only discovery.
- Verify candidate CSV records current nine-field Playnite gaps and a **potential** overlap from scanned description/cover/screenshot; it does not claim guaranteed metadata fill.
- Retain RC2 nine-field discovery and fail-closed shared-reference staging. Cached catalog identities remain read-only until live revalidation and final approval.
- Independent batch undo and controlled crash testing remain unverified; do not use the primary library for fault injection.

## v0.5.0 RC4 read-only matching gate

- [x] Preserve automatic strict matching criteria and writer/undo/journal code.
- [x] Add offline cases for review-only recall, unrelated candidate suppression, safe search queries and optional Series scope.
- [ ] Windows full WPF compile and C# test suites on user machine.
- [ ] In Playnite refresh eight false-Unmatched cases, THE FINALS and Final Fantasy X/X-2; export CSV.
- [ ] Independent single-item batch undo and crash-injection gates (deferred to isolated Playnite test).
