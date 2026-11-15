# Design C Toolbar Layout — v0.5.25 local preview

This update addresses the root cause of inconsistent control geometry in the v0.5.24 Library Workspace. It is a **read-only UI layout change**. Classic Matcher, the matching engine, IGDB requests, cache, user confirmations, metadata writer, undo journals, and existing Playnite user data are unchanged.

## Interaction/layout contract

- One four-column WPF Grid, not a WrapPanel: **expandable search** | **platform filter** | **missing-field filter** | **reset/reload utility group**.
- Shared 36 device-independent-pixel control height, 8-pixel control gap, 16-pixel separation between functional groups. Every group owns its own margins; buttons don't add a second implicit gap.
- Search icon and one-click **Clear search** are inside the text field; clear control appears only with text. Search has an accessible name and `Ctrl+F` focusing behavior.
- Platform and missing-field selectors have fixed, consistent widths/heights and accessible labels.
- A subtle vertical divider identifies the utility actions. **Reset** clears search/platform/field filters, and is disabled when all three are at defaults. It does **not** change the active category tab or erase marked games.
- **Reload** only refreshes local Playnite/cached state and retains its `F5` shortcut; it is not an IGDB scan or metadata import.
- Generic action-button dimensions are standardized, and the Previous/Next pair uses matching compact dimensions.
- No change to the navigation tabs, table columns, field preflight, preview marks, artwork lazy-loading, or Classic handoff.

## Acceptance gates (not yet proven by source checks)

1. Install preview `.pext` in Playnite and verify equal vertical heights/baselines at 100%, 125%, and 150% scaling.
2. At the workspace's minimum width and at 1500px, verify search can shrink without clipping any utility control or causing a second row.
3. Enter search text: clear button appears, reset enables; clear search restores placeholder/focus.
4. Change platform or missing-fields filter: Reset enables; Reset restores both dropdowns and search without changing tab or preview marks.
5. `Ctrl+F`, `Esc`, `F5`, Tab, Shift+Tab, and Narrator names are usable.
6. Confirm the original six-category tabs, preview-only marks, existing art, read-only history, and Classic menu continue to function.
7. No Playnite data is changed by browsing the workspace.

Do not publish or commit until real Playnite acceptance and the complete design/feature-parity release gate pass. Public main may still be older than the user's local in-progress source.
