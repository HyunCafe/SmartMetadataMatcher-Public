# Design C — Native Ignore / Restore (0.5.54)

The Design C inspector now offers **Ignore this game** and **Restore this game** for unresolved or ignored entries. Both actions require confirmation and store only Playnite game IDs through the existing `CatalogIgnored` implementation. Saved IGDB suggestions and confirmed identities are retained. No Playnite metadata, ROMs, emulator settings, import journals or undo records are edited.

Updated/completed games cannot be ignored here; recovery-required rows remain blocked. All existing marks for the affected game and any session-only draft/strict proof are discarded. The refreshed grid keeps the focused row visible, switching to All games if its previous category excluded it. Errors on malformed stored IDs fail closed.

Existing native strict batches, user-confirmed batches, history/latest undo, and Classic fallback are unchanged. Test the action in Playnite before treating runtime behavior as certified.
