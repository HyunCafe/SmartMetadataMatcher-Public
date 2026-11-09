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

1. Run Windows CI against the intended source commit and inspect its logs and candidate artifact.
2. Test that exact package in a disposable Playnite 10 profile: install/update, scan, review, cancel, approved import and supported undo. Record observed results and any untested cases.
3. Complete the relevant [release gates](Docs/RELEASE_GATE.md), including package/dependency review. Resolve failures; do not infer runtime acceptance from compilation.
4. Increment the extension and assembly version together, rebuild and verify the final package, then create an immutable release tag at its source commit. Publish the `.pext`, SHA-256 checksums and evidence with accurate beta limitations. Keep old releases unchanged.

[Historical readiness notes](Docs/DevelopmentHistory/RELEASE_READINESS.md) preserve earlier development checkpoints. They are not the current release checklist.
