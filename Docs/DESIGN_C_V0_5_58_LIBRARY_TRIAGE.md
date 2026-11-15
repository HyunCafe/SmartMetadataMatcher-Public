# Design C v0.5.58 — Console coverage and focused library triage

Three related, scoped improvements; no new metadata writer or import path.

1. **Consoles:** Exact Playnite platform aliases for PS3, Nintendo GameCube/Wii/Wii U/N64/NES/SNES, Genesis, Game Gear, Dreamcast, Saturn and Xbox/Xbox 360 map to independently published IGDB platform IDs. Unknown/combined console labels remain untrusted. The normal strict title/platform/edition/release/rival checks, exact-ID live verification, final approval and journal are unchanged. This may change which *fresh* matches qualify for strict review; runtime identity testing remains required.
2. **Source facet:** The *existing* platform selector also includes `Source: Steam`, `Source: Epic`, emulator source names, etc. Source values are read only from Playnite `Game.Source`, shown in the inspector and searchable; never used for IGDB identification. Source options are exact and preserve the current selection on reload/return.
3. **Useful metadata priority:** Existing missing-field selector adds *Meaningful gaps (not Series-only)* and *Series-only gaps*. The read-only scan processes substantive missing fields first, but still includes Series-only games. Marks, confirmation and all other filters remain unchanged.

**Limits:** No automatic import or credential change. Build and source tests are not Playnite runtime validation; use a disposable backed-up library for real import/undo and strict-match coverage. IDs sourced from IGDB platform listings/IGDB API reference and cross-checked against the published 2024 ID table; future ID changes should be reviewed rather than silently remapped.
