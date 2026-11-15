# Design C — Playnite runtime acceptance (pending)

**Status:** A source-level build, the Playnite `.pext` installer, and offline synthetic tests do not establish real metadata import, conditional undo, cancellation or crash recovery. Preserve Classic as a fallback until these cases pass.

## Required isolated environment

Use a **separate disposable Playnite 10 installation/profile** with a small test library and **back up both its Playnite database and extension user-data folder together** before any import. Prefer test games with intentionally blank metadata and copies of safe test media. Do **not** attempt these cases on the only copy of a real library or remove recovery journals as a workaround. Record the `.pext` version/hash and restore path privately; do not upload credentials or a private library export.

## Minimal acceptance sequence

| Order | Scenario | Evidence needed |
| --- | --- | --- |
| 1 | Install/update matching preview; launch Design C; inspect field-preview clicks, keyboard focus and the draft-editor prerequisite | Preview stays read-only; normal clicks do not mark/edit fields; draft unavailable until fresh exact-ID check; Classic still opens |
| 2 | Scan 3–5 mixed test titles including one unclear identity, one clean identity and one populated field | Unknown/edition mismatch goes to review; existing values remain unchanged; new entries enter without full-rescan requirement |
| 3 | Choose one explicit exact-ID identity; fetch live available fields; open draft and final comparison; **cancel before approval** | Cancel causes **zero** Playnite metadata writes and **zero** newly applied journal items |
| 4 | With backup verified, approve one missing-field import | Only approved fields are written; safe display-title cleanup is automatic where identity is eligible and must match the exact reviewed title (unconfirmed smart-inferred titles stay unchanged); ROM paths and launch actions remain unchanged |
| 5 | Close/relaunch Playnite, then **undo latest** | Changed fields restore only when current values still match the journal; history remains intact |
| 6 | Two successive approved imports; select contiguous newest-first History items and undo | Newest-first constraint enforced; older/noncontiguous selections blocked; no later manual changes overwritten |
| 7 | Modify a previously imported field manually, then attempt undo | Must fail safely or skip conflicting field with explicit warning, not overwrite manual edit |
| 8 | Explicitly confirmed batch and fresh-strict batch with at least one ambiguous/wrong-platform title | Live identity preflight, per-game outcomes, and approval gate; cancellation/partial success must match actual journal entries |
| 9 | Controlled interruption **only on the disposable profile** during an import/artwork stage; relaunch | Recovery ledger consistent or fail-closed; further writes blocked while recovery unresolved; no fabricated rollback claim |

**Stop immediately** on unexpected live writes, metadata discrepancy, missing backups, or inconsistent journal/recovery state. Save screenshots of the affected UI (redact personal data) and a small chronological log: action, expected result, observed result. Do not mark a test pass without actually observing the operation inside Playnite.

## Reporting into continuity

Record each observed Playnite acceptance result, test environment, and remaining blocker in the public release checklist or an issue, without claiming unobserved passes. Record import, undo, and recovery separately. The feature-parity gate in `Docs/DESIGN_C_FEATURE_PARITY_GATE.md` remains authoritative for Classic retirement.
