# SmartMetadataMatcher Design C v0.5.42 — verified review transfer contract

This is a narrowly scoped shared preflight milestone, **not** direct native import.

- A pure `WorkspaceReviewContract` captures an exact verified Design C selection: Playnite game ID, title, platform, missing-field mask, release year, IGDB ID/title, planned display title and a validated copy of custom draft choices.
- The transfer contract refuses stale or mixed field tables, conflicting planned titles, forged current names and invalid drafts. Its receiving-side read-only check verifies that the Playnite game snapshots did not change while the workspace changed.
- Classic receives the verified contract before it opens, as well as the existing selected IDs and validated custom drafts. Neither this contract nor the native preview can approve or write metadata.
- **Classic remains the final approval/import UI**, including backup confirmation, independent live IGDB identity recheck, journaled metadata/media updates, undo and recovery gates. Do not mistake the native field plan for the final actual write set.
- Added C# regression tests for changed source game name, missing fields, forged title, mixed identities and cross-window transfer integrity. Added a Python structural negative gate.
- The existing writer, journals, on-disk data formats, ROM files, Playnite launch paths and installed extension are unchanged by the builder.
- Guarded builder validates the exact v0.5.41 baseline, runs offline checks and Windows Release build in an isolated temporary clone, verifies the package, backs up touched source, then applies. No Git commits/pushes.
- After successful application, remove superseded SmartMetadataMatcher builder ZIPs from Windows Downloads **only if their SHA-256 matches the reviewed archive**. Other downloads, `.pext` files and backups are not deleted.

## Next milestone

Extract the final approval/import orchestration away from Classic's WPF controls, preserving the existing writer, recovery consistency and journals. Only after native end-to-end approval/import and undo tests pass should Classic be removed from the normal workflow.
