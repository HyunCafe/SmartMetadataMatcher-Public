# SmartMetadataMatcher v0.5.45 — Shared import commit coordinator

- Moves the per-game frozen native review check and *both* Classic writer call sites through `WorkspaceImportCoordinator.ApplyOne`.
- The coordinator retrieves the current Playnite game at commit time, checks the frozen Design C review if present, then delegates to **unchanged** `CatalogEnrichmentWriter.ApplyOne`.
- Existing Classic single-game and batch UI, IGDB fetch, final approval, cancellation, journals, metadata preservation, conditional undo, recovery and Playnite dispatcher remain unchanged.
- `WorkspaceImportCoordinator` can be invoked by the future Design C-only UI without depending on `CatalogWindow` methods (a `Func<Game,CatalogInput>` snapshot callback is supplied).
- **Not yet complete:** native Design C final approval or native direct import execution. Classic remains in the shipping package during migration.
- Testing: fail-closed source anchor/hash checks, full inherited Python static checks, Windows `.NET Framework` Release build, C# offline tests, package integrity. Manual Playnite review/import/undo tests remain a separate runtime gate.
- No Git commit/push/install. Source backups and rollback manifest retained.
