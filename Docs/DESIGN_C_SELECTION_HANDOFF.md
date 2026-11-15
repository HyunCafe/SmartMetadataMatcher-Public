# Design C: selected-game handoff to Classic review (v0.5.27 development preview)

This change connects **Review marked games** in Design C to the **existing** Classic review and approval path, without adding a writer or weakening live identity checks. Classic remains independently accessible through its original menu item. **No source is committed or pushed.**

## User journey

1. In Library Workspace, Ctrl/Shift-highlight rows and mark only games to process (checkboxes or **Mark highlighted**). Marks persist while filtering and are **not** approval.
2. Select **Review N marked games** and inspect the explicit ID/title/platform list.
3. Choose **Continue to Classic review**. The workspace closes and launches a **selected-only Classic session**. This ephemeral transfer contains just validated Playnite GUIDs, with a limit of 1,000 unique IDs.
4. Classic restores any valid saved matches **without granting them fresh approval** and automatically opens its existing review dialog when selected saved matches exist. When selected games have never been scanned, use **Scan games**; this session's pending queue contains **only the selected IDs**. Then use **Review matches**.
5. Cached matches still need selective IGDB refresh, ambiguous identities still require explicit confirmation, and the established import preflight, backup acknowledgement, journal, partial-failure handling, and undo are unchanged. No metadata is written until the user takes the existing explicit approval action.

**Important:** A mixed selection containing saved and unscanned games will initially show only those with results in the Classic review dialog. The Classic summary indicates how many selected games still require lookup. Close the review dialog, use Scan games for those pending IDs, then review again before applying the complete desired set. There is no automatic IGDB request or automatic import on handoff.

## Boundaries and protections

- The handoff is an in-memory, non-persisted allowlist and cannot be inferred from a title, score, or IGDB candidate.
- Classic's review source is restricted to the allowlist. It never silently appends unmarked games.
- An independent `RunBatch` guard blocks even a stale or alternate UI path from applying games outside the allowlist.
- In selected-only Classic sessions, the full-library automatic batch, full rescan, and all-cached refresh actions are blocked. The existing **selected cached matches** refresh action remains available in the review dialog.
- Completed, ignored, and historical rows are not automatically marked. User unmarking remains respected; marks are applied once per newly loaded result.
- Title cleanup is **off by default for this handoff**. Changes to Playnite display titles still require the standard separate approval path.
- Original Classic Matcher and its menu entry remain unchanged. Normal Classic launches have no selection restriction.
- This is an **intermediate functional milestone**, not the final one-window Design C review/import experience.

## Required Windows and Playnite acceptance

1. No marks: continue button disabled, and Classic opens normally from the header.
2. 2 saved matches: handoff contains exactly those two GUIDs; review opens with those two marked; none of the other library matches appears in review.
3. One saved and one unscanned: review shows the saved one; selected-only Scan games checks just the other; re-open review with both. No unrelated IGDB queries.
4. At least one cached strong suggestion: requires fresh IGDB verification (no automatic eligibility on restore). The selected-refresh action checks only the marked rows.
5. Wrong edition/platform: cannot import without identity decision and live preflight.
6. Completed, ignored, missing, or changed game: never auto-marked for new import; no missing-game writes.
7. Unmark a game in the review dialog and refresh the list: it stays unmarked.
8. Switch between category filters, Ctrl/Shift highlighting, and searching: selected-only scope persists.
9. Try full-library rescan/automatic import in a selected-only Classic session: blocked.
10. Use Classic normally from Playnite's menu: original full-library capabilities preserved.
11. Apply a safe single-game change with verified backup on a disposable Playnite profile; confirm journal/undo. Simulate an IGDB failure and a cancelled batch; no scope leakage.
12. Verify history and the existing 364 completed updates remain intact; check launch after restart.

Offline checks validate data selection, static source safeguards, existing regression tests, compiled DLL and packaged bytes. They **do not certify real-world Playnite WPF behavior, IGDB identity correctness, or crash recovery**.
