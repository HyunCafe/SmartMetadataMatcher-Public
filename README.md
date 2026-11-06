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
