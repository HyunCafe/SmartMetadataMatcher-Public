# Design C v0.5.43 — Verified field preview at the single final-approval gate

## Scope
- Preserve v0.5.42 native exact IGDB field review and Playnite snapshot protection.
- Freeze a read-only text summary from the verified native field rows.
- At Classic's EXISTING final approval window, replace speculative field labels with the verified native plan when the session was handed off from Design C.
- Reject any change in selected game IDs, IGDB identity/title, proposed display title or staged field draft between native verification and final approval.
- Re-check current Playnite missing-field snapshot before a final approval click.
- This is a transition toward Design C-only approval, **not** a second importer or extra approval prompt. The original Classic writer, live IGDB preflight, recovery consistency, media staging, and undo journals are unchanged.

## Honest limitation
The field summary reflects the last exact IGDB inspection. It does not prove image download success or guarantee all fields will be written. The Classic writer still re-fetches and may legitimately skip unavailable fields; a conflict requires re-review. Native Design C does not yet execute imports directly.

## Verification
- Static assertions for scope, identity/title/draft equality and no second writer.
- Pure C# negative tests for altered ID, title, draft and selection.
- Compile/test/package in isolated Windows copy before source update, with backup and rollback on failed application.
- Live Playnite functional testing (including undo) remains a separate gate.
