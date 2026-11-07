# Changelog

## 0.5.17 — public-readiness candidate (not released)

- Expand manually reviewed identity-confirmation batch from 50 to 1000 games, retaining explicit whole-selection confirmation and live preflight before any writes.
- Allow all strict-ready candidates to be marked even when previously confirmed selections exist; no silent truncation to five.
- Present IGDB identity-preflight progress in 50-game groups. Grouping is display-only; per-game recovery journals remain unchanged.
- Add boundary and persistence tests for 51, 431, 831, 1000 and 1001 selected identities.
- Prepare release-source, security/privacy documentation and separate Playnite add-on manifest templates.

## 0.5.16 — private development build

- Prevent checkbox double-clicks from closing the Review matches window.
- Show live identity-preflight progress for manually reviewed batches.

## 0.5.15 — private development build

- Improve PSP paired-compilation and original/port matching distinctions.

Historical development commits and private test reports are retained in the private development repository, not bundled into a clean public-source snapshot.
