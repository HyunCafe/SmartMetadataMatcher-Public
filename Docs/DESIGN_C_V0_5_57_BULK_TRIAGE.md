# Design C — v0.5.57 Bulk triage

This is a scoped UX parity release: four high-frequency actions share one compact **Bulk actions** menu in the workspace selection tray.

- **Mark all shown eligible games:** marks only presently filtered/visible items that allow review; never authorizes import.
- **Unmark shown games:** clears only visible marks, preserving marked games outside the view.
- **Ignore highlighted games:** requires a homogeneous selection of unresolved, non-ignored games; rejects updated/recovery-needed games; prompts once for the exact batch.
- **Restore highlighted ignored games:** requires every highlighted game to be currently ignored; prompts once and preserves saved matches and confirmations.
- **Show ignored games (count):** quick, exact ignored-status view for bulk restoration. It clears search/platform/missing filters, leaves review marks intact, and exits on Reset or navigation changes.

Bulk ignore/restore checks game identity/name/platform before and after approval, then re-reads the durable ignored-ID store and fails closed on changed/malformed state. It saves once via existing `CatalogIgnored`, re-reads the exact set, clears stale local marked/draft/strict-evidence state, and reloads the workspace. No metadata writer, import journal, ROM/path change, or new persistent format.

**Runtime:** Windows build and Playnite interaction remain required. Import/undo still require a backed-up disposable Playnite test before production use.
