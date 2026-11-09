# Release gates

The existing public beta remains experimental. These gates apply to each new package; a green build is not host-runtime acceptance. [Current status](../RELEASE_READINESS.md) separates available releases from source-only work.

## Build and provenance

- Build from an identified clean commit on Windows; run the complete offline test executable and source checks. Record the commit and CI run URL.
- Keep NuGet dependency versions pinned; inspect resolved dependencies and known advisories. Review package provenance separately from successful restore.
- Match `extension.yaml`, assembly and file versions. Preserve the stable extension GUID. A new shipped implementation needs a new version, not a replacement of old release assets.
- Inspect package members, compare the packaged DLL and manifest byte-for-byte with the build, and calculate SHA-256 of the actual `.pext`.
- Screen source, history changes, package content and build metadata for secrets and personal paths. Never include caches, credentials, inventories or recovery journals.

## Playnite acceptance

Use a disposable Playnite 10 library and extension-data folder, backed up together.

- Verify first installation and update from the previous beta; settings and recovery history remain usable. Test opening the UI, connection failure and cancellation.
- Scan a small mixed library; inspect strict, review and unmatched results. Include wrong-platform, edition-conflict and ambiguous-title cases, not only successful matches.
- Preview and approve a small import, verify missing-field behavior and optional display titles, and exercise supported undo after reopening. Confirm game files and launch paths are unchanged.
- Exercise strict and explicitly reviewed batches across the relevant selection boundaries. Confirm reviewed-bulk live identity preflight completes before its first write, and errors stop processing.
- Before claims about large-batch robustness, test cancellation and process interruption during writes/artwork staging, restart, inspect recovery state and verify no duplicate writes. Unknown media state must remain blocked for backup-backed recovery.
- Record observed outcomes, exact versions, package hash and outstanding cases. Do not convert earlier user observations into a current-package pass.

## Distribution

- Validate extension/add-on/installer manifests with the official Playnite Toolbox. Verify ID, API/version fields and published URLs; draft submission manifests are not proof of acceptance.
- Recheck the [official submission status](https://github.com/JosefNemec/PlayniteAddonDatabase) before submitting. Playnite 11 compatibility needs its own testing.
- Update release notes, README release status and support policy. Publish the package and checksums from the verified commit, retaining the old tags and assets.
- Perform a post-download checksum and installation sanity check. GitHub Releases is the distribution channel; no NuGet or container package is needed.

## Historical context

The [consolidation record](CONSOLIDATION.md) documents the targeted history review and retained commits. Earlier source-only and private-release checks remain historical evidence, not security certification or current runtime approval.
