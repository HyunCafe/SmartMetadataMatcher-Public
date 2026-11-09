# Security policy

## Execution and trust boundary

Smart Metadata Matcher is a Playnite .NET `GenericPlugin` executed in the Playnite process. **Playnite does not sandbox extensions.** It has the Windows-user privileges of Playnite. Do not run Playnite as administrator for routine metadata imports.

## Intended capabilities

- Query Twitch OAuth and IGDB endpoints for candidate discovery, title/platform evidence and exact-ID validation; retrieve IGDB artwork over HTTPS.
- Read Playnite game title/platform, existing metadata and extension-owned journals; write only explicitly approved supported missing metadata or reviewed display-name changes.
- Use Windows CurrentUser DPAPI to protect locally saved IGDB Client ID/Secret; OAuth access token remains in memory. DPAPI protects at rest against offline access under a different Windows identity, not against code running as the same signed-in user.
- Store scan cache, identity confirmations, import history and recovery journals in the Playnite per-extension user-data directory; these are **not** included in public release packages.
- No startup automatic import, game/ROM deletion, launch-path rewrites, telemetry or bulk remote upload of library inventories is intended by this reviewed source snapshot.

## Known limits of verification

Offline C# tests and source checks are **not** a guarantee against all malicious code, faulty dependencies or runtime failures. Each new package requires a trusted Windows build, actual Playnite 10 host tests, package inventory/hash comparison and appropriate network/dependency review. Real interrupted-write recovery in disposable data remains a separate acceptance requirement. See [release readiness](RELEASE_READINESS.md).

The project references `PlayniteSDK` 6.18.0 and `Microsoft.NETFramework.ReferenceAssemblies.net462` 1.0.3. Version pinning alone does not authenticate all downloaded NuGet bytes; dependency provenance must be reviewed before public release.

## Vulnerability reports

Do **not** paste Twitch secrets, exported game inventories, filesystem paths, logs with personal details or Playnite databases into public GitHub issues. Where supported, use GitHub private vulnerability reporting. Otherwise open a minimal issue requesting a private contact channel.

## Supported versions

The published `v0.5.17-beta.1` and current `main` are the active reporting targets. Earlier private milestones are historical and receive no promised backports. Current source may contain changes not yet available in the beta; include the installed version or source commit in reports. No SLA or security certification is offered.
