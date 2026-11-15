# Design C v0.5.55 — Selected Recent Undo

Select a newest-first prefix of applied history operations consecutively (Ctrl/Shift click), then use **Undo selected recent...**. A single itemized confirmation requires a verified restorable backup of both Playnite library and extension state. Only an exact latest-active-journal prefix is accepted; undone, older noncontiguous, duplicate and interrupted operations are rejected. Recovery ledgers are checked at preview and before each undo; the existing CatalogEnrichmentWriter.UndoLast checks each Playnite field and stops on manual-edit conflicts. On interruption, already-undone operations remain journaled and the UI reloads with partial progress indicated. Legacy v0.4 undo and advanced recovery remain in Classic pending tested parity. No automatic Playnite installation, ROM edits, or unapproved writes.

A compile/launch smoke test is not a replacement for an actual import-and-undo trial in a backed-up disposable Playnite library.
