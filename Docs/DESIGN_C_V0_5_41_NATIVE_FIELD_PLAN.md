# Design C v0.5.41 — Native itemized field plan (read-only)

## Changes

- Show the current Playnite display title and exact proposed target in native marked-batch review; retain conservative collision handling and draft rules.
- Following successful exact-ID live IGDB inspection, construct **per-game, per-field read-only proposal rows** with Playnite current values, IGDB availability, and draft-aware proposed handling. A separate native resizable viewer is available once all selected records pass.
- Bind the itemized field details to the exact same selected-game/IGDB/draft snapshots as the v0.5.40 transfer gate. Missing, mixed-ID, duplicate or stale title/field rows prevent transfer.
- Correct obsolete v0.5.36 draft-editor wording that said drafts would not reach Classic, although v0.5.38 introduced the validated transfer bridge.
- Replace inherited v0.5.40-specific version assertions with a forward-compatible manifest/assembly consistency check.
- Continue transferring to Classic's **independent final approval and existing journaled importer**. The native comparison is **not import authorization**, nor a guarantee of downloaded imagery or final written values.

## Deliberate boundaries

- No direct metadata writes from Design C. No second writer, direct database updates, changes to journal formats, or removal of Classic safety protections.
- No claimed native final approval or single-window import yet; extracting the actual shared coordinator remains next.
- No automatic bulk imports, GitHub commits, Playnite installation, or user-data changes by the builder.
- No automatic deletion of local backups or `.pext` releases. After successful isolated Windows build and local source application, only known superseded **builder ZIP** files in Windows Downloads are removed when their exact hashes match.

## Verification

- Python structural tests include no-second-writer and forward-compatible version assertions.
- C# pure-plan regression covers exact title pairs, field-detail completeness, stale status, cross-IGDB rows, duplicate rows and mismatched target titles.
- The Windows builder runs source tests, Release compilation, offline C# tests, `.pext` verification and Git-source backup in an isolated temporary working directory before changing local source.
- Live Playnite interaction and actual writer/undo behavior remain an independent user-side test gate.
