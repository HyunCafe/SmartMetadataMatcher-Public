# Design C v0.5.56 — bundled practical parity

Three grouped capabilities, one guarded build/install cycle:

1. **History review**: search and state filters; hide/restore selected rows using the exact same `HistoryVisibilityStore` as Classic. Visibility never changes journal content, undo order, media, or the Playnite database. Undo still requires newest-first, backup acknowledgement and post-approval recovery rechecks.
2. **Diagnostics and discoverability**: Advanced menu exposes existing full-library `DiagnosticExport.Run(api)` with a user-chosen CSV path, and a compact keyboard shortcut guide. No automatic exports, no credentials or ROM paths.
3. **Right-click review**: redirect selected-row actions to existing guarded native methods for ID correction, live field checks, draft editing, Ignore/Restore, History and clipboard. No new provider or writer implementation. Context targets the row actually right-clicked.

All changes preserve automatic safe Playnite display-title cleanup at approved import, no changes to ROM filenames, legacy Classic access, existing history/undo, and offline/disposable verification gates. Live Playnite UI and undo still require runtime checks.
