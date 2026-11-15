# SmartMetadataMatcher v0.5.44 — Commit-Boundary Review Validation

## Objective
Continue extracting a single safe Design C approval/import pipeline without introducing a second metadata writer.

## Change
- At the actual Playnite UI-thread handoff to `CatalogEnrichmentWriter.ApplyOne`, check the one game about to be modified against the frozen native review contract.
- Require unchanged Playnite game ID/name/platform/metadata gap mask/release year, exact IGDB ID/title, approved display title, and edited draft choices.
- Validate only the current game: prior games in a sequential batch are allowed to have completed and changed without invalidating every remaining game.
- Fail closed before the writer/journal call. Existing error handling stops the batch and explains the reason.
- Classic-only import paths (no native review contract) retain their existing writer and recovery gates.

## Safety/limitations
- No second writer; no changes to journal schemas, ROMs, launch paths or Playnite settings.
- No actual metadata writes in static source tests or isolated build. Windows and Playnite runtime tests remain separate.
- Classic remains the execution interface; extracting native final approval/import orchestration is still the next milestone.
- This is a bridge milestone, not a public release or feature-parity certification.
