# Design C v0.5.23 — coordinated usability milestone (read-only workspace)

## Status and release gate

This remains an **opt-in read-only Playnite workspace**. It does not replace Classic Matcher, transfer selections to Classic, authorize writes, perform live IGDB identity lookups, or alter files and journals. The next milestone must connect a true selected-only preflight/review/apply path through Classic's existing safety engine; do not market this preview as a production replacement.

## Completed in this milestone

- Restore familiar WPF extended selection: Ctrl+click, Shift+click, Ctrl+A, keyboard navigation. Highlighting a row only focuses it; it never authorizes writes.
- Separate **preview-only** checkbox marks from highlighted rows. Mark/unmark highlighted, clear marks, right-click actions, and selection counts. Marks survive search, category changes, sorting, and local reload for games that remain eligible. Marks are in memory only, not exported, written to disk, or transferred to Classic. A confirmation warns of this on switching to Classic.
- Existing artwork: lazily display selected game's local Playnite cover, icon, and background from the Playnite database file path; remote/absent files are not passively fetched. Limit decoding and file sizes. Missing images show empty placeholders instead of breaking the window.
- Suggested artwork: optional button fetches **only** saved IGDB cover/screenshot images from the fixed images.igdb.com CDN using validated cached image IDs. Images are explicitly labeled unverified and never authorize an import. No network images are fetched just by browsing.
- Clickable, keyboard-accessible summary cards using the same category filter; show the previously unaccounted no-gap/ignored count in the totals line.
- Search clear, Ctrl+F, Escape clears focused search, F5 reload, platform and missing-field filters, empty results explanation, sortable headings with full-text tooltips, and double-click read-only game summary.
- Improved contrast and original dark-blue/cyan WPF styling remain unchanged from v0.5.22.
- Handle failed local artwork or remote thumbnail loads without writing or suppressing failures. Preserve local game and journal safety gates.

## Verification separation

**Automatic checks:** Python read-only UX source-invariant tests, Playnite source verification, Windows .NET compilation, full offline regression suite, verified `.pext` package inventory/bytes, and source backup. These execute before updating local files.

**Still requires actual Playnite testing:** Ctrl/Shift row/checkbox behavior, sorting after marking, offline/failed images, image quality, image response latency, list/filter persistence, screen scaling, keyboard focus, text contrast on multiple themes, no extra background IGDB traffic, memory usage, and interruption/reload handling. Static checks and compiler success do not establish runtime usability.

## What remains before retiring Classic

- A true unified selected-only discovery/revalidation/review/write flow that reuses the existing strict/manual batch policy, live identity checks, field preflight, title approval, backup and journal rules.
- Live before/after per-field comparison, manual IGDB alternative search, confirmation, in-workspace history/undo, recovery status, persistent reviewed-decision continuity, and robust cancellation/error messages.
- End-to-end first-time usability test and the 40-scenario acceptance matrix in `SmartMetadataMatcher-DesignC-PreImplementation-UX-Audit.md`.

## Clean public Git history

This is **local source preparation only**; do not commit or push intermediate visual iterations. Once functional parity and host validation pass, organize unpublished changes into a small number of purposeful commits (UI feature, tests, docs) on a dedicated branch or squash before publication. Never rewrite already-published history and never commit local credentials, scan caches, or user data.

## Rollback

The builder produces a verified source backup ZIP in `Releases/SmartMetadataMatcher/SourceBackups` and a *manually installable*, locally tested preview `.pext`. Existing Classic menu entry, matching engine, scan cache, imports and journals are unchanged. Review `git diff --check` and inspect backup before reversing source changes.
