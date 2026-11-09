# Changelog

## Unreleased

- Replace game-specific matching overrides with shared title, platform, ordinal, edition and rival-evidence rules; preserve meaningful numbers and `+` markers.
- Expand discovery hints without allowing stripped prefixes or platform tags to establish identity automatically; add generalization and negative fixtures.
- Consolidate private and public histories into the existing public repository, retaining original commits and development records.
- Add Windows build/test/candidate-package automation, contribution guidance and issue forms; reconcile current release status and maintain the versioned design-decision history.

These source changes are not included in the existing beta package.

## 0.5.17-beta.1 — experimental public beta

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

The reviewed Git histories and historical development records are now retained in this repository. Historical entries describe their original checkpoints, not current release approval.

## Earlier development

The [complete development changelog](Docs/DevelopmentHistory/CHANGELOG.md) preserves the private-development entries.
