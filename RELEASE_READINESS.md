# Release readiness

The public beta **v0.5.17-beta.1** is available from [GitHub Releases](https://github.com/HyunCafe/SmartMetadataMatcher-Public/releases/tag/v0.5.17-beta.1). It predates the title-independent matching revision. Current `main` includes that revision and the consolidated development history; it is not a new binary release.

| Evidence | Status |
| --- | --- |
| Earlier Windows build and full offline suite for the matching revision | Passed in maintainer-provided output; this is not a CI run for every later commit |
| Current Windows build, offline tests and package checks | See the exact commit's [Windows CI run](https://github.com/HyunCafe/SmartMetadataMatcher-Public/actions/workflows/windows-ci.yml); a configured workflow alone is not a pass |
| Matching generalization | Synthetic invariance and counterexample fixtures; no controlled accuracy comparison with Playnite |
| Playnite 10 UI, installation/update and recovery for the next package | Required before publishing that package; earlier observations do not validate a new binary |
| Large-batch interruption recovery | Not established by offline tests; controlled host testing remains open |
| Playnite 11 compatibility | Unverified |
| Playnite add-on listing | Not submitted; manifests remain drafts |

## Next beta

1. Choose the next beta version **before** building the release candidate. Update `extension.yaml`, assembly/file versions and any version-bound validation rules together; commit the intended source, then run Windows CI for that exact commit.
2. Download the candidate artifact from that CI run. Verify its source commit, version, package inventory and SHA-256, then test **that same `.pext` file** in a disposable Playnite 10 profile: install/update, scan, review, cancel, approved import and supported undo. Record observed results and untested cases.
3. Complete the relevant [release gates](Docs/RELEASE_GATE.md), including dependency and package review. Fixes, version changes or rebuilds create a **new candidate** requiring the relevant checks again. Passing CI does not prove host-runtime acceptance.
4. After acceptance, tag the **tested source commit** and publish the **same verified `.pext` bytes**, checksums and evidence with accurate beta limitations. Do not silently rebuild or replace the approved artifact. Keep earlier releases unchanged.

[Historical readiness notes](Docs/DevelopmentHistory/RELEASE_READINESS.md) preserve earlier development checkpoints. They are not the current release checklist.
