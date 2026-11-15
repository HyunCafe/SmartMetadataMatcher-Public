# v0.5.40 — Design C verified-batch handoff gate

## Scope

- Treat v0.5.39 as the required source baseline.
- Require **every marked game** to pass the live read-only exact IGDB field check before the Design C modal can transfer to the Classic reviewer.
- Keep successful per-game verification snapshots only in memory for the modal; clear them on every new check and never persist them as identity approval.
- Prevent transfer after failed/canceled/partial checks, stale Playnite metadata, changed cached IDs/titles, title collisions, or changed field/title draft choices.
- After successful checks, allow transfer to Classic, which performs **independent** identity, title, field, backup, journal and final approval checks.
- No Playnite metadata writes, new importer, new journal, auto-approval, removal of Classic, or publication.

## Deliberately not yet completed

- Native final approval and shared write controller; v0.5.40 still transfers to Classic.
- Full field-by-field before/after comparison in native final approval.
- Replacing initial batch-size limits or adding background/unattended metadata writes.
- Remaining Classic-only tools, history management, undo, ignore/restore and recovery actions.

## Verification

- New C# pure transfer-gate positive/negative tests and Python structural checks.
- Existing regression suites, Windows .NET Framework 4.6.2 Release build, source and .pext checks run on an isolated copy **before** local source application.
- Live Playnite UI and import/undo remain user-side validation gates.

## Windows Downloads cleanup

Only after successful tests, compilation, verified package creation and local source application, the builder deletes exactly recognized older SMM builder ZIPs from the user's Downloads directory **when their SHA-256 matches an issued archive**. No glob-based deletion; unrelated ZIPs, backups, installed `.pext` and other files remain untouched. Use `--keep-old-builders` to opt out.
