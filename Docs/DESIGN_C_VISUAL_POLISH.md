# Design C — visual refinement preview (v0.5.21)

## Purpose

Bring the clear visual hierarchy and restrained deep-navy / cyan / status-color language of the original Smart Metadata Matcher into the new, opt-in library workspace. Keep the workspace's single filter dropdown, game list, and inspector rather than restoring the original crowded toolbar.

## Changes

- Familiar header with gamepad icon and a **read-only preview** label.
- Four noninteractive summary metrics: possible improvements, needs your choice, not checked yet, already updated. They show counts derived from the same existing classifier; they are **not** independent filters or claims of safe write eligibility.
- Main action is **Check or update games**, which switches into the existing Classic Matcher. It cannot update directly from the preview.
- Dark navy surfaces, restrained cyan accent, green/amber/red status text and icons. All statuses include words, never just color.
- Searchable library table with colored status icons, clearer column emphasis, better selection/hover readability and a resizable details panel.
- Inspector organizes the existing saved candidate, missing fields, and next-action rationale into separate cards.
- Existing read-only failure and recovery notices remain visible, using warning coloring.

## Scope and safety

This is a **visual-only refinement** of the previously installed v0.5.20 Design C Phase 2 read-only preview. It does **not** change status-classification rules, IGDB matching, scan caching, user confirmations, metadata writers, field editors, journals, credentials, recovery logic, or data storage.

The existing Classic Matcher menu entry stays the default and its code remains untouched. The preview has no new metadata-modifying functionality.

## Acceptance checks inside Playnite

1. The original Classic Matcher still opens and retains its existing controls.
2. The Library Workspace preview opens with the new header, four metric cards, table, and details panels.
3. Its category dropdown and search still filter the same games as before; selection updates the inspector.
4. Counts remain consistent with the v0.5.20 preview; changing the filter does not change total counts.
5. Status text is legible in both normal and selected rows and at the minimum window size.
6. The **Check or update games** button closes the preview and opens Classic Matcher.
7. No IGDB requests, metadata changes, Playnite writes or saved-data changes occur from browsing or reloading the preview.
8. A broken or malformed saved cache/journal still produces a prominent warning rather than silently reporting incorrect counts.

## Verification limitations

The builder stages the source and runs the existing Windows Release build, offline regression suite, source/package checks, and package-byte verification before patching the local checkout. Static checks alone do not establish actual WPF layout behavior, accessibility, performance, or Playnite integration; acceptance requires installing and visually inspecting the preview.
