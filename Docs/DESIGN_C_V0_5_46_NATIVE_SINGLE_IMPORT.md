# Design C v0.5.46 — native one-game import

- First direct native **one-game** execution path. Only after human confirmation of exact IGDB ID/title/platform, completed live field checks, a separate itemized final review and explicit backup acknowledgement.
- Revalidates frozen native plan and current Playnite fields before network requests, and re-fetches IGDB exact ID with structured platform. Media downloads occur off-thread.
- Revalidates the frozen plan and persisted identity confirmation again on Playnite UI dispatcher immediately before the existing `WorkspaceImportCoordinator.ApplyOne` and unchanged journaled writer.
- Stops on first error or canceled pre-write; failed/partial operations must be inspected using history/recovery before retry.
- Multi-game reviews retain the existing Classic handoff for now; Classic UI is not retired yet.
- New network/write path requires careful disposable Playnite test: one already-confirmed incomplete game, inspect exact plan, backup acknowledgement, apply, verify fields/title, undo, altered-title blocked case, canceled lookup.
- No ROM/launch-path changes, no schema changes, no second writer, no commits/push/install from builder.
