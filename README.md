# Smart Metadata Matcher for Playnite

> **Experimental public beta — not approved for the Playnite add-on database.**

Smart Metadata Matcher is a Playnite 10 Desktop extension by **HyunCafe**. It searches the Internet Game Database (IGDB) for possible matches to games already in a Playnite library, presents the evidence and potential field changes for review, and imports only user-approved supported missing metadata. It does **not** download games, rename ROM files or alter launcher paths.

## Features

- **Reviewable matching:** candidate results, title/platform/edition information, alternative matches and reasons for review. Similar names alone are not proof of a game identity.
- **Missing-field enrichment:** preview possible additions to names, descriptions and image fields, with optional title changes that the user can review. Existing populated fields are preserved by default.
- **Explicit approval:** scan/search is read-only; imports require the final change preview and an acknowledgement that a restorable backup exists.
- **Batch processing:** all freshly strict-ready candidates may be selected; manually confirmed batches support up to 1000 in one approval. The weaker supervised track remains limited to five games. Manual bulk imports validate *every* selected identity live before writing the first game.
- **Progress and recovery:** sequential per-game updates with stop-on-error behavior, individual undo records for supported operations, and guarded recovery state for interrupted media and metadata writes. Progress is displayed in groups of 50, but these are **not** separate all-or-nothing transactions.
- **Offline history:** review import history, selected undo operations, and locally persisted scan results. Saved scan results require live verification before strict import.

## Requirements

- Playnite **10 Desktop** on Windows. Playnite 11 compatibility has not been established.
- A Twitch Developer application with an **IGDB-enabled Client ID and Client Secret**. Credentials are supplied by the user; this project does not distribute shared keys.
- Working access to `id.twitch.tv`, `api.igdb.com` and `images.igdb.com` for authentication, catalog lookups and artwork.

## Installation (experimental beta)

1. Obtain the versioned `.pext` and `SHA256SUMS.txt` from the official [HyunCafe GitHub releases](https://github.com/HyunCafe/SmartMetadataMatcher-Public/releases).
2. Verify the downloaded `.pext` SHA-256 and install it into Playnite Desktop. Restart Playnite.
3. Open **Main menu → Extensions → Smart Metadata Matcher**, then configure and test IGDB credentials.
4. Back up your **Playnite library and extension user data** before approving imports. Scan, review candidate identity and proposed fields, and approve only matching games/platforms/editions.

The GitHub prerelease includes an experimental `.pext`. No official Playnite add-on database listing is claimed.

## Build from source (Windows)

The project targets .NET Framework `net462` and uses pinned NuGet references for `PlayniteSDK` `6.18.0` and .NET Framework reference assemblies `1.0.3`.

```powershell
dotnet restore .\SmartMetadataMatcher.csproj --source https://api.nuget.org/v3/index.json
dotnet build .\SmartMetadataMatcher.csproj -c Release --no-restore
dotnet restore .\Tests\NameRulesTests.csproj --source https://api.nuget.org/v3/index.json
dotnet run --project .\Tests\NameRulesTests.csproj -c Release --no-restore
pwsh -NoLogo -NoProfile -File .\Tools\Package-Release.ps1
```

The package script refuses a missing DLL or version mismatch and writes only the DLL and extension manifest to a local `.pext`; publishing and signing are separate operations. Locally built DLL/ZIP bytes are not guaranteed to equal another build's artifact. Dependency provenance and published binary verification must be established before an actual release.

## Safety and limitations

- This is a **generic plugin**, not a built-in Playnite metadata provider; it has a separate review window and user-confirmed import workflow.
- Title matches may be ambiguous (sequels, homonyms, remasters, prototypes, DLC and test clients). Weak matches require manual confirmation; no confidence score is an actual probability of correctness.
- Batch cancellation stops between games; updates already completed are **not** automatically rolled back as a group. Per-game undo is conditional on journal health and whether values were edited afterward.
- Large-batch host UI and actual abrupt-process interruption tests have not yet been completed for this release candidate. The 1000-game reviewed approval limit is a resource bound, **not** a claim that 1000-game batch recovery is proven.
- Playnite extensions run with the permissions of Playnite and are **not sandboxed**. Review the source and provenance, and avoid running Playnite as Administrator for this purpose.

See [PRIVACY.md](PRIVACY.md), [SECURITY.md](SECURITY.md), [CHANGELOG.md](CHANGELOG.md) and [Docs/RELEASE_GATE.md](Docs/RELEASE_GATE.md).

## Publisher and license

Publisher: **HyunCafe**. Licensed under MIT (see [LICENSE](LICENSE)). Not affiliated with Playnite, Twitch or IGDB. Official Playnite add-on database submissions are currently paused; this project will prepare its manifest for future submission without asserting prior acceptance.
