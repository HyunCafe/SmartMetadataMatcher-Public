# Design C v0.5.22 — dark table-row contrast fix

## Screenshot diagnosis
The v0.5.21 read-only Library Workspace screenshot showed every other game row
rendered with a white background and pale text. This is a visual/readability bug,
not an intended light theme. The existing DataGrid specified an alternating brush but did not force an
explicit DataGridRow color for either parity. WPF/Playnite styles could paint
alternate rows white. Using row-level triggers rather than a local grid
AlternatingRowBackground also prevents a precedence conflict with selection.

## Change scope
- Set AlternationCount=2 while keeping the grid itself a dark panel.
- Override each DataGridRow brush to dark; apply an explicit dark stripe
  using AlternationIndex, and put the selection trigger after it so selection wins.
- Keep selected DataGridCells transparent and use the established pale text brush.
- No changes to matching, field discovery, credentials, scanning, cache, journals,
  metadata writing, history, selection semantics, menu actions, or stored data.

## Acceptance gate
On the user's actual Playnite theme/window: all table rows remain dark; the
selected row stays a distinct blue; white text and colored status labels remain
readable while scrolling, changing filters, keyboard navigation and resizing.
Classic Matcher remains available. This package is source-tested by Windows
compilation and the offline regression suite before touching the checkout;
actual Playnite WPF styling still requires a visual confirmation.
