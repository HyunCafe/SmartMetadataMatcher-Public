# v0.5.39 — Native read-only exact IGDB batch-field inspection

This source milestone is an intermediate step toward a single Design C approval/import workflow. It is not a release gate. Windows compilation and Playnite 10 runtime verification are required.

## Included

- Design C marked review offers an optional **Check exact fields (read-only)** action. It retrieves exact IGDB records for the marked games, sequentially, with cancellation and visible per-game status.
- Every candidate is tied to a current Playnite game ID, exact IGDB ID/title, title/platform, missing-field mask, and release year. The snapshot is rechecked after network I/O. Changed/invalidated data fails closed for that row.
- Existing in-session draft choices determine which potentially available fields are displayed. Drafts remain in memory, and the established v0.5.38 transfer path stays intact.
- The review table now shows a live-check status and the potentially available IGDB fields. These are **not guaranteed writes**: media download, writable field state, identity, backup and journaling still require the authoritative import preflight.
- The check never writes Playnite metadata, creates journal entries, changes cache, imports media, approves identities, or transfers write authority. Canceled and failed checks do not block the existing Classic final review, which revalidates independently.
- New offline C# snapshot tests and static invariants; unchanged Classic writer/journal files, existing batch policy, and ROM/launch configuration.

## Deliberately deferred

- Direct Design C final approval and actual importer invocation. Classic remains a temporary visible final-review handoff, not the desired end state.
- Native exact final write plan: individual potential IGDB fields must still be confirmed against actual existing Playnite metadata and media staging immediately before journaled writes.
- Remaining Classic parity: ignore/restore, full history/undo, recovery, diagnostics, full rescan, and standalone title-only tool.
- Review of inherited user-facing development limits, public release gating, and engine extraction.

## Verification in Playnite 10

1. Open Design C, load saved matches, mark two games with confirmed identities, and open marked review.
2. Click **Check exact fields (read-only)**; verify two rows show live status and fields. No Playnite metadata changes.
3. Test one candidate whose live IGDB ID or edition changes and one whose Playnite title/platform changes during the request; the row must block without silent reuse.
4. Test custom draft field opt-outs and cancellation; the available-field list must respect opt-outs and the dialog must safely finish cancellation before closing.
5. Continue to Classic and verify the existing independent final approval, journal/recovery gates, and unchanged ROM/launch paths.
6. Recheck small/large DPI behavior; if long statuses clip, use grid horizontal scroll/tooltip instead of dropping the details.

No source modifications to the journal or writer. No Git commit or GitHub push.
