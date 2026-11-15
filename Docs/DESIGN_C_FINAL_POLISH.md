# Design C UI Alignment and Contextual Guidance — v0.5.26 local preview

This is a **read-only, opt-in preview** polish pass based on the approved v0.5.25 workspace. No matching rule, cache retention logic, confirmation store, importer, backup/undo journal, credentials, or Playnite metadata is changed. The Classic Matcher remains available from the independent Playnite menu entry.

## UX changes

- Table text, status icons and checkboxes explicitly center vertically within rows; cells have symmetric 12-DIP horizontal padding and a tighter 2-DIP vertical inset. Status/title/platform/candidate spacing is rebalanced; full text remains available in tooltips.
- The Suggestions row status is shortened to **Suggested** to reduce column width; the inspector explicitly says **Live check required** for saved, actionable suggestions. Saved matches never constitute import approval.
- **Game Details** displays correct singular/plural missing-field counts, avoids duplicated preview warnings, and gives contextual next steps (e.g. “Series is missing in Playnite. Open Classic Matcher to check whether IGDB can supply it.”). Matching availability is not guessed; no write path is added.
- The workspace-level notice explains preview limitations once. Top counts describe library scope without repeating verification warnings. The top-right button is unambiguously **Open Classic Matcher**.
- Selection actions are contextual: **Mark highlighted** when highlighted, markable games are unmarked; **Unmark highlighted** when highlighted games are marked; **Clear marks** when marks exist. The action tray shows the selected count, hidden/out-of-filter marks, and an **Inspect N marked game(s)** action that is disabled and visually subdued when the count is zero. Highlighting and checkbox marking remain distinct.
- The already-approved navigation tabs, search/filter toolbar geometry, reload, artwork expander, read-only history, previous/next navigation, Ctrl/Shift selection, keyboard shortcuts, and data safety boundaries are retained.

## Required Windows / Playnite acceptance tests

The source checks are **not** proof of actual WPF behavior. After the builder runs its Windows build and offline test suite, manually install and test the preview in Playnite.

1. At normal, 125%, and 150% display scaling, inspect the table with `Nintendo Game Boy Advance`, a long title, and `Suggested` statuses: no top-aligned text, icon jitter, white striping, or unreadable selection colors. Change column widths with drag and use header sorting.
2. Select one game, Ctrl+click two games, Shift+click a range, press Ctrl+A inside the list, and navigate with the keyboard. Highlighting **never** checks boxes by itself.
3. With one highlighted, unmarked game, only **Mark highlighted** should be available. After marking, **Unmark highlighted** and **Inspect 1 marked game** are available. With no marks, the inspection action is disabled and subdued. Marked count remains accurate when a filter hides selected items.
4. Select `112 Operator` and another game with one missing field: the inspector shows **1 missing field** and a specific next step without claiming a verified addition. For many missing fields, use **N missing fields**. Inspect a needs-review, ignored, updated, unscanned, and recovery-affected game for suitable next-step copy.
5. Navigate all six tabs, search, change filters, reset search/filters, reload with F5, move Previous/Next and expand artwork. Neither marks nor content should be lost unexpectedly; read-only browsing makes no Playnite writes.
6. Confirm the **Open Classic Matcher** action still opens the existing full workflow and warns that preview-only marks don't transfer. The independent Classic Playnite menu entry must also remain available.
7. Verify a narrow window near the minimum width and the full desktop window, with a game that has very long platform/title values. Confirm tooltips retain complete names without text overlapping another column.

**Release policy:** Do not commit/push this iteration. Consolidate unpublished source/testing work into feature-focused commits after full functional parity and real-world acceptance testing.
