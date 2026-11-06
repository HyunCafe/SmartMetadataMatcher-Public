## v0.5.8 private indexed journal replay — acceptance

- **Source baseline:** local Git commit `2ed6804d3eb561ecd04f775a0a10979afd6281f3` (v0.5.7 plus benchmark-only tooling); must be clean with stable `v0.5.0-private-stable` tag.
- **Windows gates:** full WPF compile, all offline tests (including indexed replay corruption/ordering suite), exact package verifier, 300/3000 synthetic timings, and local-only commit.
- **Previous Windows measurements:** 300 operations = 6.95 ms metadata replay, 1.04 ms media replay, 31.22 ms one journal transaction; 3000 = 144.42 ms, 12.08 ms, 218.51 ms respectively. Synthetic diagnostic only.
- **Runtime remaining:** focused history/Recovery Status inspection in Playnite; no bulk undo, intentional crash or real journal migration needed. No compiled dependency certification.
- **Scope:** no data-file edits/migration, no journal/undo schema changes; no caching of recovery state or media ledgers.

## v0.5.7 private history filtering — acceptance gate

- **Source baseline:** exact v0.5.6 local commit `59ba0870fbc74a97b5cba41e04cef05453826527` on clean `main`; retain original v0.5.0 private-stable annotated tag.
- **Scope:** pure history status/search filter, compact/full UI filter controls, long-text tooltips and selected-row details. No metadata or journal schema changes, no undo eligibility changes, no writes to Playnite or ROMs during build.
- **Automated gate:** full Windows WPF extension compile first, 14+ pure history-filter assertions, existing offline suites, release/privacy guards, .pext DLL+manifest exact verification, local Git commit after all checks pass.
- **Focused Playnite acceptance:** after installing v0.5.7, confirm All shows all operations, Applied/Undone counts are accurate, title search finds an older row, selected long title/fields are readable, and the Undo button still requires safe newest-first selection. No actual undo is required for this visual check.
- **Still not certified:** unexpected Playnite crash recovery with disposable data, large journal replay performance, independent binary/transitive dependency review, public-release suitability.

## v0.5.5 private connection test acceptance
- Source-only test: mocked Twitch OAuth + one bounded IGDB `/games` POST with explicit cancellation and sanitized error mapping; no network access from offline tests.
- Required on Windows: full extension WPF Release build, all regression suites, package byte comparison, private release report, local Git commit.
- Required for live acceptance: Test Connection with valid credentials and deliberately wrong credentials, then save/reopen without exposing secrets or changing Playnite records. No further library import or 837-item scan required.
- No automatic credential revocation, public distribution, or transitive dependency certification claimed. Keep local backups and existing v0.5.0 stable tag.

# v0.5.3 Private Clean No-Change Handling — Acceptance (historical checkpoint)

**Source baseline:** v0.5.2 `365a7754ac333e376632d59021d72de2c1a3711a` on local main; stable rollback tag `v0.5.0-private-stable` preserved. No GitHub remote.

**Behavior:** differentiate Applied, clean Skipped and Failed results in reviewed sequential batches. Zero-field, zero-external-write results may be skipped only after the media-stage ledger is durably closed clean. Unexpected upload/shared-reference activity remains a stopping error. One-game import follows the same outcome policy.

**Automated gate:** Windows WPF full build, C# offline regression suites, source/privacy guards, package byte validation, local Git commit only after success. This isolated environment cannot compile Windows WPF or prove Playnite runtime.

**Minimal live gate:** if a genuinely no-change game arises during normal use, confirm it says Skipped (no undo entry), the next approved game continues, and Tools → Recovery Status has no unresolved/inconsistent stages. Do not modify or undo successful games merely to manufacture this condition.

**Preserved:** v0.5.2 history/undo UX, v0.5.1 unlimited strict and five-item supervised policy, v0.5.0 private stable baseline, stable plugin GUID, private Playnite data/journal formats, and all ROM/launch paths. No public release claim or binary-dependency security certification.

---

# v0.5.2 Private History-UX Acceptance (historical checkpoint)

**Source:** v0.5.1 `b6c35dd1e5d9a9980032312e2f0a8b4a4fb3dce1`, local-only, clean required.
**Scope:** 10-row compact history limit removed; clear Undo metadata versus Unhide history labels; safe mixed-selection undo with already-undone rows ignored.
**Invariant:** unrestricted strict-ready selection and independent undo remain protected. No writer, IGDB matching, ROM, journal schema, or remote changes.
**Automated gate:** full Windows WPF build first, C# regressions, source/privacy checks, exact-package validation, commit only after success.
**Manual gate:** review all visible entries using scrolling; select Applied+Undone mixed entries, confirm skipped counts and undo only newest active records. Verify Recovery Status read-only, no unresolved/inconsistent stage.
**Unknown:** independently injected crash recovery, broad user-study confirmation, transitive binary security review. 30-game batch was reported successful, not proof of arbitrary 100+ batch fault recovery.

# v0.5.1 Private Batch-Unlock Acceptance (historical checkpoint)

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

## v0.5.4 private UX gate
- Confirm cached Strong rows show **Refresh required** in the main list while remaining reviewable, and only fresh strict-ready matches appear as Ready for review.
- Confirm the review button **Select all ready (N)** describes its complete scope and user-confirmed identity batches remain supervised.
- Run full Windows WPF build, pure offline suites and package verification before local Git commit. No runtime changes to writer or undo.

## v0.5.6 private scan-checkpoint gate
- Windows full WPF compile, all offline tests, package-verifier and local Git commit must succeed before installation. No remote action.
- Cache files remain under private per-plugin data, excluded from source control; v1 data contract and 7-day freshness preserved.
- Offline tests must simulate checkpoint success every ten, abrupt restart after partial batches, normal/cancelled short-tail flush and failed-save preservation of last good results.
- Manual test should be read-only: scan 12+ games, close/reopen the dashboard and confirm valid cached progress is restored. No need for a fresh 837-game full rescan or any import/undo.
- Unverified: real sudden process death/fault injection and filesystem-level persistence guarantees; do not test against the primary Playnite profile.

## Post-v0.5.7 performance instrumentation (development-only)

A read-only synthetic benchmark for the existing journal replay and durable staging paths is available at `Tools/Benchmark-Journals.ps1` (300 and 3,000 operations; optional 10,000). This is tooling-only; plugin binaries remain v0.5.7, and no journal schema or import code is changed. Actual Windows timings and any optimization decision remain pending. Large-batch performance claims require measurements, not theoretical complexity alone.
