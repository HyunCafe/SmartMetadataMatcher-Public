# Design C consolidated workspace — local development preview

**Scope:** Opt-in WPF **read-only** UX refresh atop the existing v0.5.23 source. Classic Matcher remains a separately accessible and authoritative path for scanning, live IGDB verification, manual identity correction, supervised batch limits, explicit approval, journaled writes, recovery and undo. This is a staged milestone, **not full functional replacement** and not a public release.

## What changed in the workspace

- Compact category tabs (**Suggestions / Needs review / To check / Updated / Other / All games**) replace duplicate summary-card/dropdown navigation. Counts are computed from the library; groups stay mutually exclusive. **Other** includes no gaps, ignored, and other non-primary states.
- A clear **Open Classic to scan / apply** action replaces the ambiguous "Check or update" label. **History** opens an independent read-only view of local journal records, with clear warning that historical records don't guarantee current live state.
- The inspector starts with a small **existing** Playnite cover, local title/platform, identity suggestion, a read-only possible-field comparison, and next-action text. The current/optional suggested artwork gallery is collapsed, with online IGDB images loaded only when requested.
- Field comparison keeps existing values, treats cached metadata tokens as **hints**, and labels missing metadata **Check live availability** or **Candidate may have it · verify**. It never presents tentative matches as authorized imports or claims a verified number of changes.
- Previous/Next moves among **currently filtered/sorted** games. Existing extended Ctrl/Shift highlighting and explicitly checked preview marks remain separate. A bottom tray identifies marked games, including marks outside the current filter; **Inspect marked (preview)** displays a read-only shortlist.
- Search, clearing, missing-field/platform filters, F5 reload, detailed tooltips, resize/splitter, context menus, and no-data states remain available. All preview marks are ephemeral and **never transferred to Classic or written to Playnite**.

## Explicit non-goals

- No selected GameId handoff to Classic; doing this safely requires inspecting the exact local CatalogWindow selection and preflight code, then implementing an explicit opt-in handoff and testing it inside Playnite.
- No automatic IGDB searches or saved-cache mutation from this workspace.
- No final field availability or approved before/after values until a **live exact-ID preflight** has run. "Missing" and "potential" are not a claim that data is available.
- No change to journal formats, strict/manual identity rules, title cleanup approval, writer, undo or recovery code.
- No Git commit/push or automatic extension installation. Preview version increments are **local development identifiers**, not a public series of cosmetic releases.

## Acceptance gates for the local Playnite preview

1. Search and categories account for all Playnite games; verify Suggestions + Needs review + To check + Updated + Other equals All. A previously updated game never enters the ordinary preview mark queue.
2. Confirm the inspector's current cover is correct for the highlighted game; navigate 20 consecutive games with Previous/Next, including no-cover and long-title entries. No stale remote images appear after switching.
3. Confirm the field comparison says **potential** rather than **guaranteed**, covers all nine supported fields and preserves display titles by default.
4. Ctrl-click, Shift-click, Ctrl+A, mark, unmark and clear; switch categories, search and filters; see marked-outside-this-view count and inspect the full shortlist.
5. Open the read-only History view, inspect recorded/undone entries, close it, and ensure Classic still supplies actual safe undo.
6. Test no credentials, empty cache, malformed cache/journal, and partially interrupted recovery. Errors must stay visible and never authorize metadata writes.
7. Test narrow window, 125–200% scaling, keyboard-only navigation and a Windows high-contrast theme. Check status is conveyed with **text**, not color alone.
8. Build and run all offline tests in a disposable copy, inspect exact `.pext` bytes, then test in a disposable Playnite profile. **Do not promote this preview to default** until real live matching/review/apply parity has been validated.

## Public-history preparation

Keep these iterations uncommitted or local-only. At release, review the final diff and choose a coherent release version; consolidate unpublished visual and workflow development into a small number of meaningful changes. Do not rewrite pushed commits or include local paths, credentials, user journals, screenshots, backups or draft preview packages in the public source history.
