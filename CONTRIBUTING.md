# Contributing

Use this public repository for current work. Start with a small issue or focused pull request; matching fixes should improve a general rule, not add a per-game exception.

For a map of components, data flow and trust boundaries, see [Architecture and safety boundaries](Docs/ARCHITECTURE.md). Current release acceptance is tracked in [Release readiness](RELEASE_READINESS.md); historical decisions are recorded in [DECISIONS.md](DECISIONS.md).

## Build and check

On Windows, install Git, the .NET 10 SDK, PowerShell 7 and Python 3. From the repository root run:

```powershell
pwsh -NoLogo -NoProfile -File .\Tools\Build-Candidate.ps1
```

Offline tests need no Twitch credentials or live Playnite library. Candidate packages go under ignored `Releases/`; they are not approved releases. The workflow in `.github/workflows/windows-ci.yml` runs the same entry point.

## Matching changes

- Include the failing title, platform, expected IGDB identity and competing IDs where known, using the matching issue form. Never upload your full library or credentials.
- Add a focused regression plus a counterexample: a similar title/edition/platform that must not become an automatic match. Include synthetic unrelated names when testing a general rule.
- Preserve meaningful ordinals, edition markers, ambiguous-candidate handling, fresh identity checks and explicit import approval.
- Report checks actually run and limitations. Do not equate synthetic assertions with accuracy on real games.

## UI and recovery changes

Use a disposable Playnite profile and extension-data backup for live checks. Document before/after behavior and cancellation/failure cases. Do not test destructive or interrupted writes against your primary library. Follow [release gates](Docs/RELEASE_GATE.md) for packages.

## Pull requests

Describe the problem, resulting behavior and validation. Keep unrelated formatting/refactors separate. Update current docs when behavior changes; historical records describe their original versions. Do not include keys, tokens, machine paths, private logs, game inventories or binaries.

AI-assisted contributions are welcome under the same review and testing standards. Explain what was checked; maintainers and contributors remain responsible for submitted changes.

Report vulnerabilities using [SECURITY.md](SECURITY.md), not public diagnostic dumps.
