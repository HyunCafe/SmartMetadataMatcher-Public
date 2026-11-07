# Public-release acceptance gates — Smart Metadata Matcher 0.5.17 candidate

**Status: EXPERIMENTAL PUBLIC BETA ONLY; NOT approved for a stable release or the Playnite add-on database.**

## Must pass before releasing a `.pext`

1. **Baseline:** verify actual local v0.5.16 commit, its `.pext` and source hashes; all changes introduced only on a new branch/clean source snapshot, without changing the private rollback tag or recovery journals.
2. **Build:** exact pinned NuGet dependency versions; Windows `net462` Release DLL; all offline C# tests including >50 manually confirmed records; fail-closed negative fixtures; deterministic source inventory.
3. **Batch UI:** in real Playnite Desktop test selecting/unmarking 52 and 431 entries; mixed strict/confirmed selection; close/reopen; cancel; refresh cached; identity edits; verify no accidental dialogs/automatic marking.
4. **Long-running read-only preflight:** check 51+ and 431-game preflight with live IGDB or a controlled test server, every-ID verification before first write, cancellation and first-error-stop; honor provider rate limits without silent retries that weaken identity checks.
5. **Recoverable imports:** use a disposable Playnite database and extension-data folder. Verify 1, 5, 50, 51 and a larger supervised operation; simulate unexpected process interruption mid-write and mid-artwork stage, restart, inspect journal, recover/undo, and ensure no duplicate writes or unrecoverable media loss.
6. **Security:** inspect actual compiled DLL, release `.pext` members, NuGet package provenance and transitive dependencies, secret/path scans of source and commit history, no credentials or private inventories; publish independent SHA-256 of actual release asset.
7. **Source history:** avoid converting the existing private repository to public without reviewing or rewriting the ENTIRE reachable Git history. Preferred path: create fresh public repository from this sanitized source snapshot after author approval. Never publish private local backup archives.
8. **Playnite publication:** verify stable `extension.yaml` GUID, name and assembly versions, valid `Manifests/installer.yaml` and `Manifests/addon-database.yaml`, actual release URLs, matching `AddonId`, API version, icon and screenshots with no personal library details. Validate with Playnite Toolbox when available. Do not submit while the official new-plugin intake is paused unless Playnite explicitly permits it.
9. **User-facing:** clear README, MIT license, SECURITY and PRIVACY notices, changelog, known limitations, install/uninstall, backup and recovery instructions; public `.pext` and `SHA256SUMS.txt` generated only after the passing build.
10. **Approval:** explicit user decision to publish/publicize and select MIT license/public repo name, followed by GitHub release and post-release installation sanity check.

## Current evidence

- Reconstructed v0.5.16 source from 0.5.0 baseline plus versioned development bundles; v0.5.16 update hashes verified.
- Pure Python source/RC/privacy checks passed for reconstructed 0.5.16 and candidate 0.5.17; Linux working environment has no Windows .NET/Playnite host and cannot compile or validate actual WPF behavior.
- Earlier offline suites and a 330-applied-game user history are recorded, but those are **not** a substitute for v0.5.17 large-batch interruption tests.
- Outstanding private supply-chain audit concerns may require NuGet package verification before public release.
