# Design C v0.5.53 — History / Latest Undo

Design C now exposes the existing journal's latest committed import undo with an explicit backup acknowledgement and fresh two-ledger recovery check. The established CatalogEnrichmentWriter.UndoLast remains the only undo implementation; it refuses to replace manually altered fields.

Undo is newest-only, one journal operation per approval. Older selected history, legacy v0.4.0 undo, and complex recovery still remain with Classic until separate native parity is tested. This release does not modify game files or installs into Playnite automatically.

Runtime test only with a backed-up disposable Playnite library, including manual-edit conflict and interrupted-ledger tests before production use.
