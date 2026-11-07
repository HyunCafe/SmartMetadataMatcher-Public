
## v0.5.13 private smart resolver — acceptance checkpoint
Read-only candidate enrichment now directly changes classification for a bounded set of high-evidence matches. Offline synthetic regressions exercise ordinal-prefix, original/sequel, bundle, same-name homonyms, alias and edition exclusions. Real authenticated IGDB replay across the 100 review cases and Playnite runtime import behavior remain **unverified** until Windows build and targeted fresh scan. Avoid claiming an 80% empirical correctness probability; compare actual promotions and wrong-ID counterexamples first. Existing source backups, last verified stable tag, fully reversible journal, and independent undo remain protected.
## v0.5.11 private manual identity review — PENDING WINDOWS AND PLAYNITE ACCEPTANCE

## v0.5.12 private Stage A evidence gate
- Offline: bounded candidate ID set, incomplete coverage, platform-specific year and no-write evidence tests.
- Windows: extension build, full offline suites, packaged DLL/manifest/privacy verifier required.
- Live: fresh read-only scan of representative review cases, export and compare rival diagnostics before any promotion.
- Unknown: unqueried IGDB records and classification precision on the full 100-case cohort.
- No automatic-write policy expansion, no ROM inspection and no provider secrets in reports.

- Planned: scrollable all-selected identity confirmation, explicit acknowledgement, atomic private identity persistence, bounded 50-game manual review, read-only whole-batch IGDB ID/title/platform validation before any writes.
- Existing: normal five-game legacy supervised cap, unrestricted strict-ready batch, final backup acknowledgement, serial fail-fast metadata writer, journaled undo, no ROM/path changes.
- Verified here: source/version/privacy audit, package-verifier fixtures and disposable mocked Git installer preflight/rollback.
- Not verified: Windows WPF compilation, new C# regression suites, Playnite modal behavior, genuine process-crash recovery, NuGet provenance discrepancies.
- **Do not approve 46 games merely because their titles look similar.** Verify game, edition and platform for each entry. Platform mismatches stop the reviewed batch without modifying metadata.

---

## v0.5.10 private verified-batch workflow — acceptance pending

- **Expected local baseline:** clean `ea3562db0eee5f9500e5924a6e4a006991d21f3f` on `main` with private `origin`; GitHub push is separate.
- **Windows gates:** compile full WPF extension; run offline suites plus strict-ready routing tests; run source/privacy and compiled-package checks; build a new v0.5.10 `.pext`; local commit only after validation.
- **Focused Playnite acceptance:** after installing v0.5.10, use an existing freshly scanned candidate set to verify the main **Apply verified matches...** action enters the backup-gated final review with no intermediate select-all. CANCEL without writes. In **Review matches**, Ctrl/Shift highlights multiple rows and Mark/Unmark highlighted toggles only those checkboxes. No 358-game import needed.
- **Preserved protections:** live IGDB ID/platform checks, duplicate conflicts, exclusion of saved-only evidence, user-confirmed five-game supervised limit, per-game journaling and undo. No unattended import.
- **Unverified:** real crash recovery and independent transitive dependency provenance; five NuGet hash inconsistencies documented.

---

## v0.5.9 private review-queue candidate — acceptance pending

- Based on the 837-row IGDB scan export: 330 completed, 376 no changes, 101 manual review, 28 unmatched, 1 strong candidate, 1 ignored. The previous 505 count incorrectly grouped no-change rows with review work.
- **Required Windows validation:** compile WPF and offline tests including new review-queue suite, verify package DLL/YAML, ensure no unintended repository changes.
- **Required focused host-app check:** launch the private v0.5.9 `.pext` only after backing up Playnite; confirm tab counts, visual layout, actionable-first ordering, unchanged title match eligibility and platform mismatch warning; do not rerun 706 writes.
- Scope limited to read-only UI presentation, warnings and version/docs. No schema, writer, journal or IGDB matching threshold edits.
- **State-continuity limitation:** the 376 post-apply `No changes available` rows were session-local. The existing read-only scan cache stores search statuses only, so after restart those games may reappear as saved match candidates. A v0.5.9 UI change cannot reconstruct the original 376 count without another explicit state-capture design. Do not infer new approval eligibility from any cache row.
- Old v0.5.8 release evidence audit is pinned to the *prior* package and should not be interpreted as proof for this build.
- Unresolved: NuGet dependency-cache/archive vs assets hash discrepancies (5 instances), public security provenance and genuine crash-recovery acceptance.

# Current release readiness — v0.5.8 (private, 2026-11-06)

**Current development checkpoint:** `5445fc212a0b571f0f8063111e222130be1950b3` (reported Windows Git result). The v0.5.8 Playnite package was built at `ee2ba3f8` and has SHA-256 `05C79F1AD4883D70D4ADBCB0EA86852E8F00BD189F24107AE872162EE0D9A3B4`. The subsequent recovery fault-matrix commit changes tests and documentation only; it did **not** build another `.pext`.

| Acceptance gate | Evidence | Status |
| --- | --- | --- |
| Full Windows WPF compile and offline tests | User Windows log through `5445fc21` | **Passed (reported)** |
| 30-game approved strict batch | User-reported 30 applied / zero failed, review/history screenshots | **Passed for that batch** |
| Independent undo/history | User-reported successful undo, mixed-selection UI checks | **Passed for tested operations** |
| Scan resume/IGDB connection UI | User reports v0.5.5 connection test works; offline scan tests pass | **Partly live-verified** |
| Metadata journal 3,000-record replay | v0.5.8 synthetic 80.45 ms, reduced from 144.42 ms | **Passed benchmark; synthetic** |
| Cross-ledger interruption matrix | 45 assertions on disposable files | **Passed; synthetic only** |
| Exact extension package contents | DLL + `extension.yaml` verifier and recorded package SHA | **Passed for prior Windows build** |
| Real mid-write process crash and recovery | No disposable Playnite integration test evidence | **Not verified** |
| Third-party compiled dependencies/supply chain | Declared versions pinned; independent provenance audit absent | **Not verified** |
| Compiled-binary security certification | No independent review | **Not verified** |
| Public release approval | No authorization, supply-chain proof or crash integration gate | **Not approved** |

**Private-readiness audit tooling:** `Tools/Run-Release-Readiness.ps1` recompiles and reruns all offline tests, verifies the *existing* v0.5.8 `.pext` using its exact recorded SHA-256, checks local Git status/pins and writes a private JSON report outside the source tree. `Tools/Test-Release-Audit.py` exercises negative fixtures. This tool creates **no new package, version bump, Git remote, release tag or library edits**. The audit may restore NuGet dependencies through the official configured source; it does not independently authenticate those dependencies.

**Residual risk:** Journals contain guarded recovery logic but no evidence yet that all crash windows are recoverable after a real interrupted Playnite database/artwork write. Do not intentionally crash a primary Playnite profile. A disposable profile, isolated media directory, restore-tested backup and explicit failure-point plan are prerequisites to a live crash-injection campaign.

**Historical checkpoints below are retained for traceability and may refer to earlier versions, limits or pending validations that have since changed.**

---

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

## Post-v0.5.8 synthetic recovery matrix — development-only

- [ ] Windows extension compile and all existing C# suites pass.
- [ ] Isolated `--recovery-fault-matrix` assertions pass against disposable on-disk journals.
- [ ] Read-only inspection leaves the exact synthetic journal bytes unchanged, including when malformed records block.
- [ ] Existing stable tag, real journals, Playnite data, and media remain untouched.
- [ ] **Separate live gate remains unresolved:** true interrupted `AddFile`/`Games.Update` fault injection on a disposable Playnite profile with a restore-tested backup; no test against primary profile.
- [ ] No public release or claims of crash-proof recovery based solely on this matrix.

### v0.5.11 manual review flow (private candidate)

In **Review matches**, mark 1–50 freshly scanned game suggestions and select **Confirm identities & review**. A scrollable list displays each Playnite game, platform, IGDB title/ID and evidence. Check the explicit acknowledgement **only after personally verifying every identity**, then continue to the separate final field/backup approval. The plugin saves the identity decisions atomically; no metadata changes occur until the final approval. For the larger reviewed flow, **all** exact IGDB IDs, names and platform memberships are checked live before the first write; any failure stops the entire batch without applying it. Later write errors still stop at the first failing game and preserve individual undo records for previous successes. The existing legacy five-game supervised path is unchanged.

Do not bulk confirm a candidate whose platform, edition or title you have not personally verified. Individual inspection remains available. This candidate has not yet been tested in Playnite.

### v0.5.13 optimized pre-install limits
- Pending: Windows .NET 4.6.2 compilation and offline C# regression execution.
- Pending: live IGDB response replay, 100-case review-count delta, and correctness spot-check of promoted IDs.
- Do not bulk-write before inspecting first real read-only promotion CSV.
- Original v0.5.12 commit remains the required clean baseline, with guarded backup and rollback.
- Negative fixtures include Mega Man X vs 10, English pronoun I, plus-suffixed variants, and premium-edition ambiguity.
