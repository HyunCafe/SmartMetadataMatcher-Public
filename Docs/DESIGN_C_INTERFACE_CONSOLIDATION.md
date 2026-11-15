# Design C v0.5.33 — Interface architecture consolidation

## Scope

This release stabilizes where existing features live. No new scan/matching engine, metadata writer, imported-field policy, backup gate, or undo implementation is introduced.

- **Header:** Move the existing encrypted IGDB/Twitch settings dialog and connection test to a clear **IGDB settings** action beside History. The Classic entry remains a non-primary fallback.
- **Library actions:** Bounded Scan, marked saved-result Refresh, and cancel share a compact action group. One right-aligned status line replaces a redundant preview warning band.
- **Search and filters:** Preserve the approved search/filter geometry, context-sensitive Reset and Reload.
- **Game Details:** Preserve explicit identity correction, confirmation/revocation, cached evidence, potential fields and artwork. No field is presented as safe to write until a live approved plan exists.
- **Candidate picker:** Replace the unstructured list with a read-only grid for IGDB title, platform(s), year, exact ID and title-relevance hint. Order exact official titles first, qualified editions next, partial titles next and unrelated results last; known matching platforms sort first inside each title group. This is presentation-only ranking, **not** identity confidence or import eligibility. No fuzzy result is preselected. Choosing a result still re-fetches its exact IGDB ID and asks for affirmative approval before changing the saved lookup.

## Runtime acceptance checklist

1. In Design C, find **IGDB settings** at top-right and confirm that previously saved Twitch credentials are loaded without reentry; test the connection. Verify secret remains masked.
2. Scan pending, refresh marked, Stop scan, search and filters behave as in v0.5.32; check window width near 1080 DIP and at 125–200% scaling.
3. In **The Falconeer**, open **Find / correct IGDB match**. Search. Any **The Falconeer** titles should precede *Falcon Patrol II* or unrelated F-Zero entries. Do not infer an automatic verified match from the visible labels.
4. Verify every candidate has readable columns and full-text tooltips for long games/platforms. The initial result has no highlighted selection and **Use highlighted suggestion** remains disabled until a row is selected.
5. Select the wrong result, then decline the explicit confirmation: cache, identity decisions and marks should remain unchanged. For an intended game, accept the selected ID; the saved suggestion changes only after the exact-ID fetch and Yes confirmation.
6. Confirm and forget an identity separately, and ensure neither operation changes game metadata, existing applied history, recovery journals or undo.
7. Check cancellation, HTTP failure and window closure while searching; no metadata writes or implicit approvals.
8. Confirm Classic still opens for supervised field imports, backup acknowledgement, recovery and undo; it remains a required fallback until the feature-parity gate is fully satisfied.

## Unification priority after this

Direct *actual* live field comparisons and import drafts in the right inspector; shared batch planner and backup/approval controller; journaled apply and undo/history. Avoid extending the two-UI handoff except for preserving existing fallback compatibility.

## Limitations

The display reorders up to 25 retrieved IGDB results; it does not improve provider discovery/recall or override edition, platform, release-type and identity-validation rules. Static checks do not certify WPF rendering or Playnite behavior; the user must run the isolated Windows build and test the UI.
