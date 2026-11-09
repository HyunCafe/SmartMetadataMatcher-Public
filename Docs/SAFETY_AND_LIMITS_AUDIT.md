> Historical source audit. The private/public staging descriptions below refer to the original checkpoint. See [current readiness](../RELEASE_READINESS.md) for the published beta, consolidated history and remaining release requirements.

# Safety, testing and limit audit — 0.5.16 baseline to 0.5.17 candidate

**Scope:** Reconstructed `net462` Playnite 10 `GenericPlugin` source. This audit distinguishes **source evidence** from **offline test evidence** and **real Playnite verification**. **Not a public-release approval.**

| Control or limit | Existing behavior / evidence | Public-candidate decision | Required acceptance |
|---|---|---|---|
| Read-only scan | IGDB candidate discovery with no Playnite writes; persisted cache | **Keep** | Rescan/cancel in actual Playnite with unchanged metadata |
| Cache age | Previous matches cached up to seven days; cached entries do not grant strict write permission | **Keep** | Live refresh and stale-cache negative tests |
| Scan checkpoints | Read-only cache flush every ten completed scan records | **Keep** | Force abrupt scan termination; validate resume and corrupted cache handling |
| Strict-ready batch cap | No count cap in pure policy; requires fresh strict evidence | **Keep uncapped** | 400+ real UI selection, no incorrect/archived entries |
| Manual reviewed cap 50 | Literal limits in review validator, batch validator and atomic identity storage | **Expand to 1000** as bounded resource envelope | 51/431/831/1000 positive, 1001 and corrupt-row negative tests; actual WPF host |
| Weak-evidence supervised cap | Five per approval; cannot inherit strict authorization | **Keep five** | Mixed/unconfirmed negative tests |
| Mixed strict + manual selection | Previously only five ready additions when an identity already confirmed | **Remove premature selection restriction** | Review every identity; no silent skipping; final 5/1000 gates still apply |
| Manual identity confirmation | Full-list confirmation with explicit acknowledgement before identity store write | **Keep** | Verify source title/platform/ID unchanged and no saved decision on failure |
| Identity store | Versioned JSON, atomic file replacement, max 10k records, 2 MiB, duplicate/invalid rejection | **Keep** | 431-record persistence + byte-identical rollback on 1001/duplicate failure |
| Bulk IGDB preflight | Every manually confirmed ID/title/platform checked live before **any** Playnite write | **Keep all-or-nothing preflight** | Large-batch mismatch at last game must produce zero writes; cancellation supported |
| Identity preflight pace | 350 ms between requests to respect IGDB traffic | **Keep** | Live provider-rate-limit and timeout testing; no speed bypass |
| 50-game internal groups | No actual 50-game processing transaction existed; progress only | **Show progress groups** without claiming chunk transactions | If true chunk transactions needed later, implement and test independently |
| Final approval | Proposed fields, title/platform/edition review plus backup acknowledgement | **Keep** | Confirm/Cancel, empty selection, edited draft, title collision |
| Field writes | Missing-field-only assisted matching; configurable, guarded title updates | **Keep** | Verify existing populated fields, custom artwork and ROM/launch paths unaffected |
| Journal writes | Per-game prepare/commit, media staging, restart recovery checks | **Keep** | Actual process interruption at multiple writer boundaries in disposable Playnite data |
| Failure behavior | First unexpected network/preflight/write failure stops remaining games | **Keep** | Errors leave identifiable partial result, block new writes if recovery needed |
| Undo | Journaled per-operation restore with state/ordering checks | **Keep** | Multiple successful games followed by failure; newest-prefix undo and edited-field conflict |
| Export | User-triggered local CSV of candidates, contains library titles | **Keep with privacy warning** | Redact private game inventories and paths from reports/screenshots |
| Credentials | Windows `CurrentUser` DPAPI for locally stored Twitch ID/secret; token in memory | **Keep** | Confirm no secret in source/release archive/log or export; DPIA-style privacy review |
| Network | Fixed Twitch OAuth, IGDB API and image CDN endpoints | **Keep** | Network interception/review of request shape, errors and unexpected redirects |
| Dependencies | Version pins in csproj; prior private NuGet audit identified remaining package-provenance questions | **Release blocker** | Verify provenance, package hash and exact compiled binary package bytes |
| Package | Previous `.pext` contains DLL and `extension.yaml`; tests check ZIP inventory and private markers | **Keep** | New actual release built, SHA256SUMS independently recorded, Toolbox verification |
| Public Git history | Private repository includes internal test and version history | **Do not simply flip privacy switch** | Publish reviewed clean-source snapshot with clean history, or explicitly approved complete history audit |
| Playnite submission | Separate add-on listing and installer YAML; Playnite currently pauses new plugins | **Prepare now, submit later** | Verify actual URLs/IDs, images, version and portal availability |

## Regression acceptance tiers

**A — Safe for private source candidate:** read-only static source checks, exact baseline and ZIP hashes, offline source/privacy checks, installer clean/tampered baseline rejection and no-delete rollback design.

**B — Must pass on Windows:** entire `net462` WPF compilation, all offline tests including 51/431/831/1000/1001 cases, package inventory and first actual Playnite 10 review UI checks.

**C — Must pass before public release:** real user-data-safe crash and cancellation testing in a disposable Playnite library; restore from backup; 51+ live preflight, no writes on final preflight failure; dependency provenance; published release DLL/`.pext` byte inspection; source history verification; actual Playnite Toolbox validation.

**Important:** B and C are **not** established by the Linux source prep. The 50-game grouping is merely progress presentation, **not** transaction partitioning and not a promise that completed games are rolled back together.
