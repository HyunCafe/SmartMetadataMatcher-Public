# Design C — Guided selected review preparation (v0.5.28)

## Scope

This milestone removes the manual prepare/scan/refresh/reopen loop after selecting games in Design C. It **does not** provide an independent metadata writer or automatic import. The Classic review dialog remains the only final approval path. The normal Classic menu entry remains unchanged. No commits or public release.

## New flow

1. In the Design C workspace, mark games and inspect the explicit selection list. Click **Continue to Classic review**. The dialog explains that live IGDB requests may follow, confined to those game IDs.
2. Classic opens in a selected-only session and starts read-only preparation. **Pending games are searched first**, then **selected cached matches are rechecked**. Fresh matches are not needlessly queried again in the same preparation.
3. If more than 50 selected lookups/refreshes are required, a separate Yes/No prompt discloses the network work before it starts.
4. A compact status message reports progress. **Stop after current game** cancels the active scan or refresh; errors and cancellations stop the automatic sequence. A **Prepare selected review** button permits an explicit retry.
5. When all selected preparation steps succeed, Classic opens the existing restricted review dialog, with games still unapproved. Manually review editions, platform, alternatives, and proposed fields, and approve only the desired records.
6. If a refreshed catalog identity changed, its mark is cleared, and the user is notified before the review dialog. Identity changes are never silently carried into a batch.
7. Actual writes, backup acknowledgment, live exact-record preflight, batch limits, journaling, history and undo continue through the existing Classic code. The selected-game allowlist is independently enforced at `RunBatch`.

## Specific guardrails

- Handoff has a maximum of 1,000 distinct, non-empty Playnite game GUIDs, never a title-based search scope.
- No automatic metadata writes, title cleanup, identity confirmations, or full-library rescans.
- The normal Classic window is unaffected and has no guided prep lifecycle.
- Saved scan results remain suggestions, not strict eligibility. They require fresh search and final live preflight.
- Cancellation or a network error stops auto progression, retains safe partial read-only scan results, and allows manual review/retry.
- Malformed cache/journal/credential state still follows the existing fail-closed behavior.
- All changes remain local until a separately authorized Git commit/push.

## Acceptance checks in actual Playnite

1. **Saved only (2 games):** selected-only session refreshes just those two and opens review. No other library game queried or marked.
2. **Pending only (2 games):** lookup runs only for those two, then review opens.
3. **Mixed (one saved, one pending):** pending lookup runs, then saved match refresh, then one review dialog containing both.
4. **No network needed:** review opens directly without IGDB calls.
5. **No IGDB credentials:** informative pause with settings guidance; no provider calls or writes. Configure credentials and retry.
6. **Large selection (>50):** explicit Yes/No before network requests; No does not scan.
7. **Cancel during scan/refresh:** preparation stops without opening the approval review automatically. Manual retry is available.
8. **Provider exception and partial result:** no automatic transition to final review; existing partial completed lookups remain inspectable.
9. **Changed IGDB ID or edition:** affected mark cleared; visible warning; no automatic approval.
10. **Completed/ignored/outside-handoff game:** cannot enter the selected import batch.
11. **Final approval:** verify exact ID/platform, backup and field diff again in disposable Playnite profile. Verify undo from history.
12. **Normal Classic menu launch:** original behavior, including full-library scans and tools, unaffected.

The source assertions and offline C# tests validate control routing and regression safety, not WPF/IGDB behavior in the Playnite host. A Playnite disposable-profile acceptance run is still required before treating the workflow as validated for real metadata writes.
