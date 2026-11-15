# Design C v0.5.34 — IGDB candidate row-selection fix

The IGDB chooser in v0.5.33 already uses a single selected whole row, but WPF can still draw bright, separate focus borders around individual DataGrid cells. This makes one selected game look like five independently active fields.

## Changes

- Remove individual cell borders and cell-specific focus rectangles in **this chooser only**.
- Show one consistent deep-blue selection across the full selected game row, using the existing Selection brush.
- Preserve single-selection, read-only candidate columns, keyboard Up/Down navigation, manual choice, and exact-ID revalidation.
- Preserve the main library's independent multi-highlighting/checkbox marking behavior.
- Do not change IGDB search relevance, identities, caches, saved confirmations, import writer, backup gate, journals or undo.

## Playnite acceptance

1. Open Find/correct IGDB match and search for several games.
2. Initially no row is selected; the Use highlighted suggestion button remains disabled.
3. Click the text, platform, year and ID cells on different results. Each click selects the **entire row** with one consistent background, without a bright blue box around individual cells.
4. Press Up/Down to move the selected row and verify the choice button follows the selected game.
5. Confirm that closing does not change saved identity; accepting a suggestion still invokes the exact-ID check and separate explicit confirmation. No metadata update occurs.
6. Inspect the regular library grid: Ctrl/Shift highlighting and independent marked checkboxes should still work as before.

The installer performs an isolated Windows compilation and full offline tests before changing source, creates an exact source backup, and refuses mismatched baselines. Static checks do not certify visual rendering inside Playnite.
