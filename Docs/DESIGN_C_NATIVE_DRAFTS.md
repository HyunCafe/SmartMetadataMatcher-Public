# SmartMetadataMatcher v0.5.36 — Native Design C draft proposals

## Purpose

Add an optional **Edit proposed metadata** action to Design C after an explicit exact-ID IGDB field check. Keep the existing Classic importer, backup, journal, matching, identity confirmation, history and undo unchanged. This is **not yet the import/approval feature**; local draft choices do not flow to Classic.

## Behavior

- Field-by-field checkboxes for missing Playnite fields where the exact IGDB record has a potential value. Existing or unavailable data is shown but cannot be checked for a replacement.
- Independent opt-in **proposed Playnite display title**, validated using the existing `CatalogImportDraft.ResolveTitle` collision and title rules. No changes to ROM filenames, game paths or launcher actions.
- Independent opt-in **custom description**, using the existing length/content checks. This can replace an existing description only if explicitly approved in a future import step.
- **Save draft**, **Reset draft**, **Cancel**. Drafts are in memory only, per game; restarting Playnite or closing the workspace discards them.
- Returning to a game shows the draft's included-field count and annotates excluded fields in the inspector. Highlighted/marked game selection is not affected.
- A draft is tied to exact Playnite game ID/name/platform, supported missing-field mask, local release-year snapshot, exact IGDB ID/title. If those change, the draft becomes inapplicable and is discarded. Before saving, fresh Playnite and saved IGDB cache preflight must pass.
- The comparison and edit controls are disabled while scanning or checking IGDB fields. No implicit remote calls on row selection.
- No metadata import, journals, persistent decisions, caches, IGDB provider or Game database writer are modified by this milestone.

## Acceptance tests

1. Open Design C, select a saved game, and click **Check IGDB field availability**.
2. Click **Edit proposed metadata**, uncheck one possible field, and save. Confirm the inspector reads **Excluded from this draft** and retains the game mark state.
3. Reopen the draft; excluded fields remain unchecked. Use Reset draft and confirm the status clears.
4. Try a duplicate Playnite title; saving must fail with a collision notice. A valid title should save.
5. Test blank custom description (rejected), reasonable custom description (accepted), and leaving the custom description option off (no override).
6. Choose a different IGDB identity, rename the Playnite entry or change its supported metadata; old draft must not follow the changed identity/snapshot.
7. Test cancelling the dialog and closing/restarting the workspace: no Playnite fields should change, and session-only drafts must not persist after a restart.
8. Confirm Classic Matcher and approved import/backup/undo still work as before. The Design C draft does **not yet** affect Classic review or apply.

## Next milestone

The next step is a guarded native **batch review / approval integration**, where selected games and their draft preferences must pass the existing exact-ID revalidation, live data preview, backup acknowledgement, supervised batch policy, journal integrity, cancellation and recovery gates. The current draft UI must not imply this is already implemented.

## Verification boundaries

The builder validates source fingerprints and runs source/static tests, the full offline C# suite, Windows .NET Release build and package checks in a disposable directory before changing the checkout. Offline fixture tests are not proof of a Playnite runtime or compiled binary security certification.
