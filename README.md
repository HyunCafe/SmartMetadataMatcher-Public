## v0.5.16 private review-window correction
The 50-game manual review dialog no longer closes when checkboxes are clicked rapidly; use Inspect game to navigate intentionally. After approving a reviewed bulk batch, the window now shows a live, read-only IGDB identity preflight counter before any metadata writes begin. Identity validation, network pacing, final approval, backup, journal and undo safety rules remain unchanged. This private build needs Windows and Playnite validation before public release.

## v0.5.15 private focused matcher

This update targets two read-only identity decisions (explicit numbered compilations and undated same-name ports). It does not claim to resolve generic Final Fantasy IV PSP editions, Legend of Heroes numbering, PC same-title homonyms, ROM-only titles, or wrong-platform provider records. Run Windows regression tests, install privately, and compare export IDs before authorizing writes.

## v0.5.14 private — Distinctive subtitle and platform-port matching

Read-only scan reranking can now promote a uniquely supported game whose local title omits a franchise wrapper, a PSP series ordinal or an episode wrapper. The selected IGDB ID must still match the Playnite platform, carry a supported game type, survive complete rival lookup and pass live ID preflight; no overwrites of existing metadata are enabled for assisted matches. Generic titles, conflicting editions, test clients, unknown platforms and incomplete searches remain in Review. ROM headers and store IDs are not read by this release.


## v0.5.13 private — Smart candidate selection
Fresh scans compare IGDB's platform-specific exact-title and prefix results instead of trusting the first fuzzy result. A distinct best candidate can enter the existing verified-matches batch, with an auditable reason and heuristic lead (not a true probability). Prefix automation requires a numbered multiword game, a verified base-game type and a nontruncated query. Different editions, DLC, identical-title games with no independent distinguishing date, and unresolved rivals remain in Review. Saved scan results never regain automatic eligibility without refresh. Automatic assisted imports fill only missing metadata; leave **Use catalog title** unchecked. Final live ID checks, backup acknowledgement, per-game journals and undo remain mandatory.
# Smart Metadata Matcher — v0.5.11 Private Candidate

## v0.5.12 private — read-only candidate evidence
When a fresh scan marks a game **Needs review**, Smart Metadata Matcher makes at most one bounded lookup of up to 12 competing IGDB IDs and records game type, version/parent relationships and platform-specific release years when available. Details appear in match diagnostics and CSV reasons. The investigation does **not** change the proposed game, automatic eligibility, write decisions or approval flow; IGDB search coverage is never assumed exhaustive. No ROM or executable files are read. Offline unit tests do not prove live IGDB classification accuracy.

**Manual-review fix:** checked, unresolved IGDB matches can now be confirmed together in a scrollable identity review (maximum 50 games) and then proceed to the same backup-gated final approval. Checkboxes alone are not proof of identity. The reviewed batch checks **every** IGDB title and platform live before any metadata write; if one fails, the batch stops without writing. Earlier five-game supervised flows and strict-ready automation remain unchanged. This candidate requires Windows validation and a small live Playnite check.

---

## v0.5.10 private workflow — apply verified matches directly

After a fresh read-only IGDB scan, use **Apply verified matches...** from the main dashboard to include every strictly eligible match without first marking hundreds of games in Review matches. This performs the existing exact IGDB ID/platform/field verification, shows a scrollable final plan, and requires both a verified backup acknowledgement and explicit final approval before any Playnite writes. A saved or ambiguous match is not auto-approved.

Use **Review matches** to handle ambiguous identities, saved results needing refresh, or manually chosen subsets. Ctrl/Shift/Ctrl+A highlights multiple rows; **Mark highlighted** and **Unmark highlighted** change the checkboxes separately from highlighting. User-confirmed identity matches still follow the supervised batch safeguards.

---

## v0.5.9 private — Actionable review queue (2026-11-06)

- After a 837-row real scan and a 706-game import (330 applied, 376 safe no-change skips, zero reported failures), the dashboard separates *Candidates / Needs review / Unmatched / No changes / Completed / Ignored / All*. No-change entries no longer inflate manual-review counts.
- Needs review sorts games with candidate metadata overlapping currently missing fields first, followed by lower-opportunity reviews. This is only an advisory presentation sort; no ranking confers approval or changes matching thresholds.
- The preview flags a high heuristic score when IGDB's canonical title looks different after known platform-aware prefix normalization, and separately flags reported platform mismatches. Existing manual identity and strict write gates remain intact.
- Existing game metadata, on-disk journal formats, scan cache, recovery behavior and ROM paths are unchanged. Current v0.5.8 dependency provenance limitation remains unresolved; this private build does not claim public readiness.

## Private release evidence (v0.5.8)

The latest local release-readiness status is in [`RELEASE_READINESS.md`](RELEASE_READINESS.md). After the guarded recovery-test milestone, run `pwsh -NoProfile -File Tools/Run-Release-Readiness.ps1` from the project root to verify existing compiled package bytes, dependency version declarations, full offline suites and local-only Git provenance. It creates a private report outside the source tree but no new `.pext`, release tag or remote. This is not a supply-chain or actual Playnite crash-recovery certification.

## v0.5.8 private — Faster journal history replay

The enrichment journal replay now indexes operation identifiers in memory while parsing to avoid repeated linear scans. The index exists only during a full, verified replay; journals are always reread from disk and retain the exact prior on-disk format, ordering, legacy timestamp support, and fail-closed semantics. Media staging remains unchanged. Run the synthetic 300/3000-operation benchmark after building to compare with the v0.5.7 baseline; existing data is never used.

## v0.5.7 private — Find updates in history

- Use **Show: All / Applied / Undone** above the dashboard's Update history list. All is the default and no 10-row cap exists.
- In **View full history...**, filter by state, search game titles/changed fields/dates, and optionally include hidden entries.
- Hover over a truncated title or field list, or select one operation in Full History, to read its complete details.
- **Undo selected metadata** restores the newest consecutive applied changes only. **Show in update history** simply unhides selected records, without changing Playnite fields.
- These filters are read-only. Private diagnostics and per-game journal entries are unchanged.

## v0.5.3 — Private clean no-change batch handling (pending Windows/Playnite acceptance)

- A live IGDB item with **no actual safe metadata or title changes** is now **Skipped**, not a hard batch failure. No Playnite update and no field/undo record are created; the empty media stage is durably closed clean before proceeding.
- Skip is permitted **only when no media upload and no globally shared metadata Add operation was attempted**, no field change is reported, and the clean stage closure succeeds. Anything uncertain still fails closed, stops the batch and requires recovery review.
- Batch progress and summary distinguish **Applied / Skipped / Failed**; a skipped row is unmarked and labeled `No changes available` until refreshed. Single-item imports show the same outcome without falsely reporting a completed operation.
- Preserve Playnite title/platform/identity rechecks, strict batch criteria, five supervised limit, backup approval, journal compatibility and per-game undo. No ROM, emulator, media deletion or remote changes.
- Regression coverage checks all eleven possible field flags, staged-media/global-reference uncertainty, invalid counters and correct total progress; live WPF+writer compilation and Playnite runtime remain unverified here.

## v0.5.2 — private update-history clarity and safe mixed-selection undo (pending Windows acceptance)

- The dashboard **Update history** now lists every visible committed operation with scrolling and virtualization, removing the hidden 10-row cap. Hidden entries remain accessible in Full History.
- Full History distinguishes **Undo selected metadata** (changes Playnite records) from **Show in update history** (only changes local history visibility). The latter is disabled until hidden entries are selected.
- Mixed history selections may contain Applied and Undone rows. Undo ignores already-undone operations and processes only the selected newest consecutive *applied* prefix after reading the live journal. Selected older nonconsecutive applied records are explicitly left unchanged, with counts in confirmation and result messages.
- All database safety rules remain: latest-first undo, existing manual-edit checks, global journal gate, stop on first undo error, per-game journal history, no ROM/path changes. **No existing journal data is migrated or rewritten.**
- RC version/package labels updated; automated mixed-selection regression assertions added. No Playnite UI/runtime validation is claimed until Windows build and manual check pass.

## v0.5.1 — private batch-unlock candidate (post-stable, pending Windows/Playnite acceptance)

- The v0.5.0 private-stable source at `c1b08d1` and the local `v0.5.0-private-stable` tag remain the rollback baseline.
- Remove the fixed 25-game cap for **independently strict-ready, freshly validated** matches. Select all remaining ready entries or manually choose a subset; no silent truncation.
- Keep the **five-game supervised** limit for user-confirmed/weaker-evidence matches and the one-at-a-time manual exception path. Strict evidence, cached-result refresh, duplicate game ID checks and explicit final approval are unchanged.
- Keep live exact-ID/platform/title rechecks, sequential per-game writes, stop after first failure, cancel-before-next-game, per-game undo journals, recovery blocking and Playnite/ROM isolation.
- **Do not mistake removing the testing cap for proving crash recovery or large-batch performance**. No unattended import, cross-run auto-resume, or unbounded retry is added; test a backed-up modest batch before a large one.
- This is a **private development candidate** until Windows build/offline suites and focused Playnite runtime verification pass. No GitHub publication.

## Private RC3 — Precision and export audit (2026-11-06)

- Corrected false edition warning for ordinary titles such as *Hogwarts Legacy*. Explicit `- Legacy` and `Legacy Mode` remain review-sensitive.
- Exact alias matches with a material remaster/edition/client difference now require identity review (e.g., *The Falconeer* and *Realm of the Mad God Exalt*); matched edition titles are unaffected.
- Rejects implausibly low-similarity candidate recommendations such as *THE FINALS* → *Final Fantasy IV*, retaining manual search; adds search-only article-free and collection punctuation variants.
- Candidate CSV now includes nine-field Playnite gaps at export time, count, potential fills supported by scanned artwork/description evidence, and cached-result status. Scan evidence does not guarantee a write or validate the match.
- No ROM, emulator, import writer, journal schema, five/25 batch-policy or Playnite library changes during build.
- Existing RC2 checkpoint `91148ae` remains the rollback target. Windows compilation and live IGDB search remain unverified until performed locally.

# Smart Metadata Matcher — v0.5.0 private RC2

The current personal-use **release candidate** preserves the verified v0.4.32 importer and batch/recovery behavior while strengthening release checks and reproducibility. **This is not a public release or a claim of production-scale validation.** See [RELEASE_READINESS.md](RELEASE_READINESS.md) for the verified features, unresolved runtime cases, and rollback gates.

**Build:** `pwsh -NoLogo -NoProfile -File ./Tools/Build-Preview.ps1` (Windows). The full extension compiles before offline suites, the `.pext` is checked against the DLL and manifest, and the private release report is written by the guarded upgrade after the local commit. No Playnite database or ROM access is performed by the build.

Historical notes below document older stages; their former test limits and unfinished-state statements do not supersede the RC1 acceptance matrix.

---

## v0.4.32 — Preflight and package verification

A final read-only selection check at import dispatch rejects duplicate Playnite game IDs, archived records, invalid catalog identities and stale strict-ready evidence. The build runs `Tools/Verify-Package.py` on the actual .pext, rejecting unexpected ZIP members, missing manifest/DLL, source-to-package byte mismatches, and known private path/identifier markers. Release verification is **not** a security certification; an isolated multi-game import/undo Playnite test is still required.

### v0.4.30 review filter correction
Editing a staged title/description/field draft from the Review Matches context menu retains the existing filter (including **Selected for import**) and highlighted game. Only selecting a different IGDB identity can reset the review filter. The change does not affect metadata writes, undo, or ROM files.

Read-only search fallbacks for Legacy, Beta/Test, Playtest, Starter Access, launcher, SDK, selected DLC, and Ys I & II are discovery-only; these editions always require review.
### Full-library matching safety (v0.4.26)
A full 318-result CSV audit prompted conservative handling of alternative-name-only matches: an exact IGDB alias linking a completely unrelated official title requires review, even with a corroborated platform ID. Examples include Bump Bomb / Flame Beetles and Manic Troll / Coin Digger. Score 1.000 is not a correctness probability. Cached results still require fresh IGDB evidence; use **Refresh cached** in the review dialog for selected saved games.


### v0.4.26 — Refresh cached matches in place
If a saved scan looks Strong but the import review says Refresh required, mark those rows and click **Refresh cached** in Review matches. This rechecks only the marked cached rows in IGDB without modifying Playnite. Unchanged title/ID selections remain marked; changed matches are deselected for another review. Batch imports still require separate approval and verified recovery state.

### v0.4.25 private recovery checkpoint
Run `python Tools/Verify-Source.py` to check version labels, compile references and test-suite wiring without changing files. The normal Windows preview build calls this verifier automatically. Media staging and journal field IDs are now cross-checked before new writes. The five-game test limit remains until one complete import-and-undo is tested inside a disposable Playnite profile; use the official Playnite backup tool to preserve Settings, Library, Library media, Extensions and Extensions data first.


## v0.4.23 — unified final approval and recovery visibility (private preview)
- Replaced the separate backup prompt and single-line manual confirmation with one scrollable final approval window shared by manual and strict planning. User must confirm a restorable Playnite library and extension-data backup before Apply is enabled.
- Included the current Playnite title, platform, candidate, exact IGDB ID and intended Playnite title for every proposed import. The list is a planning preview; the exact live IGDB ID/platform and field state are still revalidated before writing.
- Added per-game batch progress, explicit stop-after-current wording, and a close-window safeguard while an operation runs. An interrupted/ambiguous journal remains fail-closed.
- Added Tools > Recovery status, a read-only inspection of unresolved media stages and uncommitted enrichment operations. It cannot delete or repair files automatically.
- Retained supervised five-game validation limit and per-operation journals; no production bulk unlock until Playnite import/undo and crash-recovery testing.

## v0.4.21 identity continuity
In Review matches, highlight a candidate and use **Confirm highlighted identity** to remember the exact ID for that unchanged Playnite game. Confirmed entries appear after reopening when the same game, title, platform, catalog title and IGDB ID still match. Use the same action to forget a confirmation. No confirmation silently applies metadata; live ID/platform verification and user approval remain mandatory. Existing private scan cache and recovery history remain independent.

## v0.4.14 — History selection and approved Playnite title corrections (private preview)

- Recent and full history tables support Ctrl-click, Shift-click and full-row extended selection.
- History can rename previously imported titles inside Playnite, with explicit confirmation for same-platform duplicate display names; it never changes ROM paths, executables, or launch configuration.
- Selective undo is limited to the newest consecutive journal operations, in reverse order; stale/manual edits fail closed.
- Unattended and supervised metadata-import collision protections remain unchanged. Build and live Playnite testing required.

> **0.4.13 private preview:** Scan settings now open as a small dropdown. The metadata preview emphasizes artwork and possible before/after changes; detailed comparison and diagnostics can be expanded. The matching and write engines are unchanged. Test the UI before production writes.

### v0.4.6 unified dashboard
Playnite: **Extensions → Smart Metadata Matcher → Open Smart Metadata Matcher...**. The dashboard's **Tools** menu contains the full-library metadata diagnostic CSV exporter, the existing dedicated title-review window, and the full metadata-import history.

After a confirmed metadata import, the dashboard opens the **Completed** tab and keeps the processed game selected. **Inspect IGDB fields** performs an optional, exact-catalog-ID read-only lookup to show whether IGDB currently supplies genres, developers, publishers, series, and platform-specific release dates; it also shows current values and whether an import would preserve or fill them. No search-time requests are added by this control.

The standalone title-review tool opens as a separate modal after the dashboard closes; any unapplied current-session scan results are not persisted. The existing confirmed one-to-five-game import and per-game undo remain unchanged.

# Smart Metadata Matcher — private development preview v0.4.1

**Purpose:** automatically identify games and eventually fill reliable Playnite metadata with minimal manual review. This milestone tests **one confirmed game at a time**, not bulk automation.

## What's new

- Confirm a Strong IGDB candidate manually and **apply the full available enrichment** in one operation: corrected Playnite display name (optional), missing description, cover, icon (IGDB square thumbnail), and background (IGDB screenshot). The original ROM/EXE and Playnite game actions are never changed.
- Never overwrite populated media/description fields. Preserve custom sorting names. Prevent same-platform title collisions; if a collision exists, skip *only the rename* while allowing metadata fields to import.
- Missing IGDB artwork is skipped, not fabricated; an unavailable background is not a reason to reject an otherwise successful import.
- **Needs attention / Completed / All results** selector. Completed games hide from the active view, appear under Completed, and are recovered from the private journal after reopening, provided the recorded fields still match the live game.
- Refresh the selected row immediately on apply/undo. Legacy v0.4.0 description/cover undo remains supported; a separate v2 write-ahead journal covers the expanded fields.
- Existing IGDB scan sizes **20 / 50 / All remaining**, testing-only exclusion, direct `/v4/games` discovery, source diagnostics, name-only review and undo all remain available.

## How to live-test safely

1. Back up **Game Library + Extensions Data**, ensure the backup ZIP exists, and save prior known-good `.pext` packages.
2. Run `pwsh -NoLogo -NoProfile -File ./Tools/Build-Preview.ps1` in the project. Install the new timestamped package, restart Playnite.
3. In **Extensions → Smart Metadata Matcher → Find catalog matches / test one metadata import**, scan 20; choose ONE independently verified Strong result with a known platform/edition.
4. Leave **Use catalog title (if no collision)** checked to test naming. Before approving, read the exact IGDB identity, candidate, platform and collision warning.
5. Apply one game. Confirm its Playnite Name, Description, Icon, Cover and Background under **Edit Game Details → General / Media**, and check it moves to Completed. Missing IGDB screenshots may result in no background.
6. Click **Undo last metadata import**, verify every changed field returns, and the row returns to Needs attention. Reopen the window to test persistent Completed history on a later test.
7. Do not bulk apply or publish: v0.4.1 has NOT been compiled or live-tested on the target Windows machine.

**Credential privacy:** Twitch Client ID/Secret remain stored in Windows-user DPAPI via the existing credential store. No secrets, ROM paths, private Playnite inventories or user settings are embedded in the release.

**Important constraints:** The heuristic Strong label is not a probability. Only the explicit single-game action can write catalog metadata. Downloads happen *after* user approval and before the Playnite database update. Release-source/compiled-package provenance requires a separate audit before public GitHub publication. Work in a development branch for any eventual publication.

---

## Historical development notes (read only when needed)

# Smart Metadata Matcher — Private v0.3.0 Catalog Discovery Preview

## Current scope and safety

- **Now supports explicitly approved Playnite-only game title changes.** Nothing changes just by scanning, marking, filtering, or copying proposals.
- Always back up the Playnite library before trying the rename feature, and test with ONE distinctive, non-colliding game first.
- **Rename marked...** opens a full scrollable confirmation list. It rechecks live title/proposal/status before the operation. Same-platform/source collisions remain blocked even if marked for manual review.
- **Undo last batch...** uses a persistent private write-ahead journal saved inside Playnite's plugin data directory. Only an unchanged exact-match result can be restored. Do not delete the plugin's user-data directory while undo is needed.
- No ROM filenames, executable paths, play actions, metadata fields, installed-game files or source artwork are changed. No startup scanning or network activity is introduced.
- **Not yet live-tested** in Playnite; compilation must run on your Windows machine. Source test success does not establish an operational database restore.
- Dark-theme explanatory labels now use Playnite's `TextBrush` resource. Platform values have hover tooltips and a wider column.
- Pre-existing features (diagnostic CSV export, name-rule review, metadata-only queue, filters and bulk-selection) are retained.

## First live test (after compile/package)

1. Back up the Playnite library using Playnite's backup feature.
2. Install the timestamped v0.2.0 `.pext`, restart Playnite, open the review window. Verify text contrast and platform tooltip.
3. Filter by `Ready`, **mark one non-colliding title**, inspect the full confirmation preview, apply, and verify that only the visible Playnite name changes.
4. Reopen/refresh, select **Undo last batch...**, confirm, and verify that the exact original name returns. Check that the emulator/PC launch entry is unchanged.
5. Try a colliding title and confirm that the checkbox is disabled. Test a manual `Review` candidate ONLY when its identity is already verified. Bulk changes remain deferred until this single-game round trip succeeds.

## Build (Git Bash, inside project)

```bash
pwsh -NoLogo -NoProfile -File ./Tools/Build-Preview.ps1
```

The .pext contains only `SmartMetadataMatcher.dll` and `extension.yaml`. Its name includes the exact timestamp and version; older builds are preserved.

## Limitations

- The plugin does **not** download external metadata or prove catalog identities. It only displays missing-core-field status and proposes title cleanup.
- Preview classifications are heuristics, not probabilities. Manual review remains essential for ambiguous names, abbreviations and aliases.
- Undo is **name-field-only**, latest batch first. If the name was changed again afterward, the plugin refuses to overwrite it. A journal record may exist when a database operation was interrupted; recovery compares the actual current name.
- There is no transactional all-or-nothing database rollback; per-title errors stop processing, and already applied entries are recoverable through the journal if their names remain unchanged.
- Avoid public release before a Windows compilation and actual Playnite install/rename/undo regression test.


## Prior design/development notes

# Smart Metadata Matcher — Private v0.1.2 Read-Only Preview

An offline-first Playnite 10 extension for examining messy game titles and proposing safer names. **This first checkpoint is read-only**: it does not update game titles, ROMs, launch paths, metadata, other fields, or local game folders. It does not automatically scan on startup or access the network.

## First working checkpoint

- Load the Playnite library only when the user opens **Main menu → Extensions → Smart Metadata Matcher → Preview game title cleanup (read-only)**.
- Propose removal of two-digit numeric collection prefixes (`01 Pokemon Fire Red` → `Pokemon Fire Red`). Treat as **Ready** only when at least three consecutive numeric prefixes exist within a known common platform and source; otherwise **Review**.
- Move trailing articles (`Legend of Zelda, The - Oracle of Ages` → `The Legend of Zelda - Oracle of Ages`) as **Review** suggestions, without guessing the official catalog title.
- Normalize excessive whitespace; preserve numerical sequels, three-digit IDs, version tags and edition labels.
- Downgrade suggestions to Review if a different entry would share the same proposed title in that platform/source group.
- Show original/proposed titles, source, platform, status and reasons; use search, status/platform filters, Ctrl/Shift/Ctrl+A row selection, one-click mark checkboxes, mark/unmark highlighted, check all **visible Ready** rows, clear visible rows, refresh, and copy marked plan to clipboard.
- Marking does **not** approve or apply any rename in this preview build.

## Build/test on Windows

Prerequisites: Git Bash, PowerShell 7 (`pwsh`), .NET SDK, network access to NuGet for first restore, Playnite 10 for UI testing. Package versions match the existing published HyunCafe Playnite extensions: PlayniteSDK **6.18.0**, .NET Framework **4.6.2** reference assemblies **1.0.3**.

Run from the `SmartMetadataMatcher` directory in **Git Bash**:

```bash
pwsh -NoLogo -NoProfile -File ./Tools/Build-Preview.ps1
```

This compiles and runs the pure-title-rule test program, then builds the actual Playnite extension. It produces a timestamped `.pext` in sibling `../Releases/SmartMetadataMatcher/` without overwriting previous builds. The release package includes only `SmartMetadataMatcher.dll` and `extension.yaml`.

Install the `.pext` through Playnite, restart Playnite, open the new menu item. Verify that the window opens, shows plausible suggestions, filters work and selection marks on first click. **No actual renaming can occur in this build.** Do not publish v0.1.0 yet.

## Known intentional limits

- A two-digit prefix can be part of a genuine game title; a three-title sequence is supporting evidence, **not a calibrated 90% probability**. No renaming is enabled yet.
- This version does not use external metadata catalogs, aliases, cover art, ROM checksum identification, file changes, duplicate deletion, automatic source correction, title editing, or full automated matching.
- A game's platform or source can be unset or inconsistent; these cases remain in Review.
- The initial rules deliberately avoid speculative spelling, Pokemon/Pokémon stylization, article-to-colon conversions, title abbreviations, and release-edition inference.
- The UI may reveal actual game names and source labels to local viewers. No such inventory is included in the source package.

## Planned next checkpoint

After successful local build and UI test: add user-approved Playnite-only `Game.Name` changes with a persistent, guarded undo journal; keep all unrelated fields and ROM files untouched. Then integrate the handoff to the user's configured Playnite metadata providers.

**Provenance:** Written using Playnite SDK official documentation and published HyunCafe/DuplicateReview and HyunCafe/LocalGameImporter source as implementation references. Not compiled or installed in the model's environment; local tests and UI verification are pending.


## v0.1.2: metadata triage (read-only)

- The main review window now includes games with missing cover, background or description,
  even if no rename is suggested. Rows with all three fields present and no rename are hidden.
- Status: Ready/Review = proposed title change; Metadata only = title unchanged,
  needs missing metadata. Only rename proposals can be marked or copied.
- Core metadata filter: All / Missing all / Missing some / Background only / Complete.
  "Complete" here means these three fields have stored values, not verified accuracy.
- Added PSP-suffix removal only for Sony PlayStation Portable/PSP platform entries,
  plus a few conservative manual-review title alias candidates. No external catalog
  lookup is performed and even a plausible alias is not proof of game identity.
- No Playnite database writes, no renamed ROM files, no network activity, no metadata
  downloads, no publishing. Full diagnostic CSV export remains available.
- If the plugin window does not reflect changes, verify the installed .pext version
  and restart Playnite. Test filtering and classifications on representative games.


## v0.3.0: Read-only IGDB catalog discovery

- New menu: **Smart Metadata Matcher → Find catalog matches (read-only)...**
- This is a foundation for the approved automation objective: search IGDB, rank plausible
  identities by normalized titles, catalog aliases and corroborating platforms, and show
  whether candidate description, cover and screenshots are available.
- Enter *your own* free Twitch Developer application's Client ID and Client Secret locally.
  **Never paste the secret into chat or a GitHub repository.** The user-entered credentials
  are encrypted using Windows CurrentUser DPAPI and stored in Playnite extension user data.
  Twitch app access token is retained only in memory during the scan.
- The plugin accesses `id.twitch.tv` for OAuth and `api.igdb.com` for game searches only after
  clicking Scan. Queries send the chosen game title to IGDB, not ROM paths or inventories.
- **Scan next 20 missing-core-metadata games** performs a bounded, cancellable read-only
  discovery pass. Review the titles, platforms, match scores, catalog IDs, and field coverage.
  The scores are *heuristics*, NOT probabilities of correctness. Download the report CSV using
  the window's Export button to share candidate quality without exposing credentials.
- No changes to Playnite metadata, artwork, names, or files via this catalog window. The
  pre-existing v0.2.0 name-only rename/undo functions remain available in their own window.
- Windows C# build, Playnite UI and real authenticated IGDB requests are not proven by
  generating this patch. Run the test/build script and validate a small actual catalog batch.

### Deferred intentionally

- Automatic metadata application (including Playnite media file import) is **NOT implemented**
  in this preview. Before allowing it, audit candidate accuracy against representative ROMs,
  protect existing metadata, keep a field-level rollback journal, and test with one game.
- Expanding from 20-item samples to rate-limited incremental bulk processing, additional
  providers, catalog cache, fallback searches, and file-hash identification will follow
  actual measured gaps. Avoid unsupported claims about 90% accuracy.
- IGDB terms, non-commercial eligibility and any use of game descriptions/artwork in public
  distributions must be checked before a public release.


## v0.4.0 private controlled metadata-write test

- Catalog scanning and CSV remain read-only. After scanning, select exactly **one** Strong candidate and click **Apply selected (ONE game)...**. Verify the precise IGDB title, platform and edition in the confirmation.
- The plugin re-fetches the IGDB record by ID, checks the platform and that the Playnite game has not changed, and fills **only missing description and cover**. It does not rename games or modify ROMs in this workflow.
- The cover is fetched over HTTPS from IGDB and imported into Playnite's media database. A separate private append-only metadata journal records original values before the game is updated. **Undo last metadata import...** restores fields only if they have not been edited again.
- This is an experimental **single-game-only** writer. It does not auto-apply all Strong candidates. Treat catalog scores as heuristics; verify matches before confirmation. No automatic metadata changes occur when scanning.
- Back up Game Library and Extensions Data in Playnite before live testing. First test one missing-description/cover game; then undo and verify both fields.
- C# build and unit checks do not prove that the network, image API, UI, or undo work on your Windows installation. Do not attempt bulk processing until a successful live test.

## 0.4.2 supervised batch preview

Check one to five *Strong candidate* rows with one-click Mark checkboxes, or highlight rows and choose **Mark highlighted**. Highlighting alone never triggers writes. Choose **Apply marked (up to 5)** and review the full before/after list and any title collisions. Confirm to apply sequentially. Each successfully applied game receives its own undo journal entry. Errors on individual games are reported; other rows are preserved. Cancellation stops *future* work and cannot undo a game already applied; use **Undo last metadata import** repeatedly to restore the last applied changes. Review and Unmatched results are not eligible. This is **not** unattended automatic metadata import. It is a controlled first batch test before wider automation.

The visual layout of a future one-window catalog reconciler is a concept, not implemented as the mockup. External metadata fields beyond name, description, cover, icon and background are deferred until sourced and tested independently. Existing valid metadata is not overwritten.


### v0.4.3 dashboard
The dashboard provides tabbed catalog views, a selection preview, local ignored IDs, and applied history. Images in the preview may load from the official IGDB image CDN for the selected candidate only. Existing Playnite enrichment writes remain limited to name, description, cover, icon, and background, and require explicit confirmation (up to five games). Other metadata fields shown in future design mockups are not yet applied.

### v0.4.4 WPF interface update (development preview)
- Larger dark themed header, structured tabs, compact toolbars with Windows-native icons, improved typography and information spacing.
- Resize the **entire metadata preview** by dragging the vertical splitter between game table and preview; resize **recent history** by dragging the horizontal splitter beneath the game table.
- The right preview contains a current-versus-suggested metadata table with individually resizable columns; double-click a field or choose **Open full comparison** for a separate sizable inspection window.
- The preview is informational: catalog descriptions and additional genre/developer/release fields are not yet applied until the later metadata-writer milestone.
- All data writes remain supervised and limited to 1–5 game entries with journals and rollback; keep a Playnite backup.


### v0.4.5 metadata fields
After explicit confirmation of a Strong catalog candidate, the plugin may additionally populate *empty* release year (only when IGDB supplies a matching-platform date), genres, developers, publishers and series. Existing values are never replaced. The undo journal records these field changes; old journals remain compatible. Shared library genre/company/series objects created by name are not deleted on undo, since they may be reused by other games. Metadata previews use the scan's limited payload; exact additional field values are fetched by ID when applying. The scan and its scores are never proof of correctness or an authorization for unattended writes.


## Strict auto-plan preview (v0.4.7)
1. Back up the entire Playnite library, including game library and extensions data.
2. Scan catalog candidates (20, 50, or All). Review the candidate results and platform details.
3. Click **Auto-plan next 25...**. This chooses at most 25 strict matches; ambiguous, colliding, repeated-ID and previously failed results stay out.
4. Read the entire scrollable approval list. Approve only identities you recognize. One click authorizes this batch, not unlimited updates.
5. Watch sequential progress; **Cancel** prevents new games after the current item. Applied games persist in Completed with individual journal entries.
6. Undo works newest-first; for a larger batch, repeat Undo to reverse each game. Validate all changed fields and previously present art in Playnite before expanding further.

This is NOT hands-free unattended writing. An exact-title/IGDB platform match is an engineering gate, not a correctness guarantee. All metadata import functions keep existing values, files and Playnite launch settings intact.


### v0.4.9 private UI preview
The primary toolbar now opens **Review plan** to display **read-only** strict identity eligibility and review reasons. It does not perform exact live IGDB field-level preflight or authorize writes. IGDB credentials are configured in **IGDB settings**. Technical columns are optional under Tools. To avoid regressions during the transition, older limited write actions remain available under **Tools → Legacy supervised testing** and must not be treated as the production bulk workflow. No v0.4.9 Playnite behavior has been validated yet.

## v0.4.10 — marked review and supervised test UI

- Review dialog opens on marked games when available, otherwise ready games or all matches; no unexplained empty first view.
- Inspect button has an auto-selected row, disabled state for empty categories, and explanatory no-selection messages.
- Strictly ready marked games can be forwarded to the **existing** exact-ID revalidated batch writer only after a Playnite backup confirmation and full second approval. Private testing limit is five marked games; no silent truncation.
- This is NOT the v0.5 durable unified job runner, not an accuracy certification, and not permission for production bulk imports.
- The Playnite metadata writer, journal format, IGDB transport, installed extension, and public repositories are unchanged by this source patch.

A single marked Strong candidate that is not strictly auto-ready may be tested using the existing supervised one-game path only after an explicit personal identity and backup confirmation. Multiple marked games must all be strict-ready.


### 0.4.11 — CSV-driven title retrieval and identity safety (private preview)
- Audited **Super Mario Adv** on GBA resolves to Super Mario Advance; no generic `Adv` trust.
- Verified redundant acronym removal (e.g. `WDL - World Destruction League`), preserving nonmatching acronyms.
- Exact complete-token permutations enable reordered edition/main-title matches while retaining every digit; unique same-platform catalog identity and dated record remain mandatory for automatic planning.
- Search-only fallbacks for titles ending in `Adv` (Adventure/Adventures), informal `1` numbering and numbered series subtitles. Search-only variants cannot independently authorize strict automation.
- Fire Dragon (undated), bare Mario Kart, wrong-platform Pokemon Yellow, rival sequels and duplicate exact IGDB records continue to require review.
- The matching evidence is still not calibrated probability; **no modifications to enrichment writer or journals**. Live IGDB quality and Playnite tests are pending.

### v0.4.12 private UI preview
Scan games, then review and mark candidates in the same dialog (search and strict-ready marking available). The visible identity plan is not a verified field-level write plan. Supervised imports still require an explicit full Playnite+extension-data backup and final approval. Production bulk automation is not enabled.

### History (v0.4.15 private preview)
New operations record UTC timestamps and display local apply dates. Undo is retained as a read-only history state; older v2 log entries with no timestamp display Unknown (legacy). Undo remains latest-consecutive only. No ROM paths or emulator settings are modified by the Playnite display-title correction action. Test backups and exact-field undo on your own Playnite library before additional bulk imports.

### Private v0.4.16 preview notes
Recent Updates supports a reversible **Remove from recent** action; use Full History > **Show hidden** > **Restore selected** to recover hidden entries. Hidden applied records still count toward stack undo safety. **Fix Playnite titles** operates only on already committed identities, proposes a `(GBA)`-style suffix on same-platform collisions, requires confirmation and affects Playnite display/sort names only.

### Title cleanup in the normal import (v0.4.17)
The approved metadata import also cleans the Playnite display name. Numbered and abbreviated game names become readable canonical titles; existing same-platform titles trigger a platform suffix (GBA, GBC, etc.) where unambiguous. The complete before/after title is shown before approval and included in the normal undo journal. This never renames ROMs, folders or launch targets. Ambiguous identity or multiple title collisions remain review-only. The separate history rename remains a recovery/legacy option rather than the normal workflow.

### Simplified selection (v0.4.18)
Highlight one or more results (Ctrl/Shift) and use the single Select highlighted / Deselect highlighted button. Select visible ready toggles only eligible games visible under the active filter; it never marks hidden games. Alternatively check rows directly. Review & apply displays the number selected. Clear all and Ignore/restore are under More. The review dialog presents selected games and requires the same separate backup and game-by-game approval before supervised import.

### Direct review/history controls (v0.4.19)
The results toolbar exposes Select highlighted, Select visible ready, Clear selection (when useful), Ignore/Restore highlighted, and Review & apply. Recent Updates has Restore changes (undo) and Remove from recent directly; neither action permanently deletes the journal. Full History restores hidden rows. Undated exact matches may become ready only with additional structured platform/context evidence; all writes still require approval.

### Resumable catalog scans (v0.4.20 private preview)
Candidate results from the past seven days load automatically if the Playnite game name, platform, and missing-field count are unchanged. A restart or cancelled scan does not discard already completed read-only requests. Use Tools > Rescan all to archive saved results and request fresh IGDB searches. Cached results are not auto-write authorization, are never auto-marked for import, and must be checked against IGDB before a supervised import. Cache data stays in private plugin user data; neither credentials nor launch paths are stored.

### Interrupted artwork staging (v0.4.22)
The plugin records artwork staging separately from its field undo records. If an import was interrupted before its media references were durably handed to the undo journal, later imports are blocked. Do not delete `media-staging-v1.log` or guess which Playnite-managed artwork to delete. Inspect the recorded operation and restore from a verified Playnite/extension-data backup where necessary. This preview does not provide automatic crash cleanup.

### v0.4.24 Recovery Consistency (private preview)
The read-only Recovery Status panel now verifies closed artwork handoffs have matching metadata intents, and blocks new writes on orphan/mismatched records. The final approval displays potential changes inferred from the previous scan. Actual imported fields depend on a fresh exact-ID fetch and live Playnite preflight; it does not rename ROMs. Keep using small supervised tests until recovery is verified in a disposable Playnite profile.

#### Equivalent game duplicates (v0.4.27)
If two Playnite entries match the same exact IGDB title and console, the duplicate ID alone no longer blocks supervised enrichment. The plugin automatically proposes a console-suffixed Playnite display name when a clean title exists elsewhere in the library (including a different console). Every game still requires independent identity evidence, live platform verification, backup acknowledgement and approval; ROM filenames and launch paths are never renamed. When even the suffixed name collides, the original display name is preserved pending manual cleanup.

#### Right-click game review (v0.4.28)
Right-click a row in the dashboard or Review Matches list. You can open Playnite's native Edit Game dialog for title and metadata fields, inspect read-only current/IGDB comparisons, fetch the complete IGDB record by ID, or search alternative IGDB candidates. A chosen alternative requires structured console evidence and explicit confirmation. This does not import metadata by itself; the normal supervised import still revalidates the exact catalog ID/platform and asks for final approval. Playnite-native manual edits are saved directly by Playnite and are not part of the Matcher undo journal; the row is marked for revalidation before another Matcher import. ROM files and launch paths are unchanged by the Matcher actions.

### In-app proposed edits (v0.4.29)
Right-click any unmatched/ready review row and choose **Edit proposed import**. Customize the proposed Playnite display title or replacement description, and uncheck fields not to import. The same editor is available from Review Matches and the final import dialog. The edits are ephemeral until explicitly approved and applied, and reversible using Matcher’s normal journal. The Playnite native editor remains available for fields not supported as inline custom values. Existing fields remain untouched except explicitly opted-in description replacement.

### v0.4.31 — One review flow for supervised batches
In Review Matches, use **Select next 25 ready** (or hand-pick a smaller group) to select up to 25 independently **strict-ready** games and choose **Review & apply selected**. If the batch contains a manually confirmed identity, the supervised limit remains five; a non-ready Strong result can be imported only through the existing single-game manual confirmation path. The final approval shows each intended game, IGDB ID, console, title and fields, with mandatory backup acknowledgement. Import stops on the first failed game; previous successes remain journaled and undoable. Verify 2–5 games in Playnite before relying on the 25-game preview mode.

### RC4: lower-noise read-only discovery

- Candidate selection can show **Other metadata** (default), **All missing fields**, or **Series only**. This only changes display and visible-row selection; no Playnite records are modified.
- Series-only games remain in the scan queue, sorted after games missing other fields.
- Low-similarity same-platform title anchors may appear for **manual review** but never gain automatic import eligibility.
- When ordinary search yields no usable match, the plugin can try one bounded exact-name IGDB request before returning Unmatched.

### Understanding match statuses (v0.5.4 private)
- **Ready for review**: fresh independent evidence can enter final review, *not* a guarantee that every field will change.
- **Refresh required**: a saved Strong candidate must be revalidated from the review window before strict batch approval.
- **Confirmed — review**: previously chosen identity is still subject to exact live IGDB/platform verification and approval.
- **Needs review / Unmatched**: game/edition needs human intervention or no suitable identity was found.
- **Select all ready (N)** in Review Matches selects every currently strict-ready match, not a limited batch. Clear/mark individual entries to import a smaller subset.

### Verify IGDB setup (private v0.5.5)
Open **IGDB settings** and enter your Twitch Developer application's Client ID and Client Secret. Click **Test Connection** to check Twitch OAuth and a minimal read-only IGDB game query without saving. On success, click **Save settings**; credentials are stored using Windows CurrentUser DPAPI, but access tokens are kept in memory only. Saving without testing is supported; the banner will correctly say the connection is untested. The test never imports metadata or uploads your Playnite library.

## v0.5.6 — resumable read-only scans

Long scans now save candidate results to the existing private cache after every 10 completed games, not just when the batch ends. If Playnite exits unexpectedly, reopen the Matcher to reload still-valid saved results; the last incomplete checkpoint may need rescanning. Ordinary cancellation and IGDB errors also save the final partial set. Results are read-only evidence and **never** grant permission to import without current live verification. Use **Tools → Rescan all** only to intentionally archive past cached results and start fresh. No changes to ROMs, launch settings, or Playnite game metadata during scanning.

## Private journal performance diagnostics

From the project root on Windows with PowerShell 7, run `pwsh -NoProfile -File ./Tools/Benchmark-Journals.ps1`. The optional `-Large` switch adds a synthetic 10,000-operation case. This builds and tests the real v0.5.7 source, then benchmarks only disposable synthetic enrichment/media journals. It does not access Playnite's database, existing private journal data, ROM files, or network. The command does not create another `.pext`, bump the extension version or install anything. Timings depend on local hardware and filesystem; collect results before proposing journal replay optimizations.

### Development-only recovery fault matrix (post-v0.5.8)

Run `pwsh -NoProfile -File Tools/Test-Recovery-Faults.ps1` from the project checkout to compile the full plugin, run all offline checks and then replay the focused interruption matrix. It writes only synthetic logs in unique temporary directories, never contacts Playnite, and never packages a `.pext`. The suite covers durable event boundaries, not a real killed process, filesystem corruption during flush, or actual Playnite database writes.

### v0.5.11 manual review flow (private candidate)

In **Review matches**, mark 1–50 freshly scanned game suggestions and select **Confirm identities & review**. A scrollable list displays each Playnite game, platform, IGDB title/ID and evidence. Check the explicit acknowledgement **only after personally verifying every identity**, then continue to the separate final field/backup approval. The plugin saves the identity decisions atomically; no metadata changes occur until the final approval. For the larger reviewed flow, **all** exact IGDB IDs, names and platform memberships are checked live before the first write; any failure stops the entire batch without applying it. Later write errors still stop at the first failing game and preserve individual undo records for previous successes. The existing legacy five-game supervised path is unchanged.

Do not bulk confirm a candidate whose platform, edition or title you have not personally verified. Individual inspection remains available. This candidate has not yet been tested in Playnite.

### Private v0.5.13 optimized smart candidate ranking
The fresh read-only scan re-ranks complete IGDB same-platform rivals, handles safe numbered-title variants (including `I`/`1`), and retains richer duplicate-ID evidence. A promoted match still needs live identity validation, final approval and journaling. If evidence is incomplete or candidates remain genuinely ambiguous, the game stays in Review.
The matching key retains plus-marked edition suffixes, and incomplete names never select a Premium Edition by omission.
