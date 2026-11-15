# Design C — Phase 2: real Playnite read-only workspace

## Intent

This is an **opt-in parallel workspace** launched through a second Playnite menu entry: **Open Library Workspace (preview; read-only)**. The classic Smart Metadata Matcher entry remains first and fully functional. A live Playnite library snapshot (not sample HTML data) is categorized through `WorkspaceTaskClassifier`. No automatic scans, imports, IGDB requests or journal writes happen in the preview.

## How to test

1. Install the generated `.pext` with Playnite closed, restart Playnite, then open **Extensions → Smart Metadata Matcher → Open Library Workspace (preview; read-only)**.
2. Check that completed games (the prior 364 when still valid) appear as **Already updated**, not in **Possible improvements**.
3. Compare **Not checked yet**, **Possible improvements**, and **Needs your choice / attention** against Classic Matcher. Cached results must be displayed as suggestions, not approved updates.
4. Search by title and platform, change categories, select games, and inspect missing fields. No edits should occur.
5. Use **Open Classic Matcher to check or update games** and verify that the existing full UI still works, including scan, manual review, batch approval, history/undo, and Tools.
6. Reopen preview after a new ROM has been imported into Playnite: it should show up as **Not checked yet** if it has incomplete supported fields and no reusable scan result.
7. Close/reopen preview: journal history, ignored IDs, and cached lookup data must not change.
8. Test with an invalid cache or journal in a disposable Playnite test profile: the preview must display a safe error, not claim zero history or falsely mark everything unscanned. **Do not corrupt real user files for testing.**

## Feature boundary

The first preview shows a **live read-only inventory**, not an interactive edit pipeline or comprehensive field-by-field proposed-diff inspector. Saved IGDB evidence is displayed but never treated as strict authorization. A real preflight that checks additional IGDB fields is a future phase; this view does **not** label such cases as safe-to-apply or confirmed no-change. Full feature parity remains a release gate. Previous `v0.5.19` API workflows are not removed, remapped, or bypassed.

## Safety and publishing

- Same extension GUID; the preview build uses a new numeric version `0.5.20`, clearly separate from local `0.5.19`.
- The source builder runs the full staged Windows build/tests, package byte comparison, source inventory and privacy checks **before** touching the local checkout.
- Original edited source is backed up and atomic writes are rollback-tracked. Existing local modifications are not silently deleted; the two Phase 1 classifier files must match their original installed bytes.
- No Git commit/push, public release or Playnite installation is automatic.
- Offline tests cannot certify the real Playnite host, all keyboard/screen-reader interactions, or first-time usability. Preserve Classic as the default until verified.
