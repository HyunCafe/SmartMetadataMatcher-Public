# SmartMetadataMatcher v0.5.49 — Scan All Pending without bulk writes

Design C now offers **Scan all pending** beside the existing **Scan next 25 pending** action. When any games are marked, each scan action operates only on marked pending games; otherwise it operates on all pending games. All requests still run through the established `WorkspaceCatalogLookup.Run` service with at most 25 games per call, existing IGDB rate limits, cancellation, and persistent lookup cache checkpoints. The status shows progress across the full chosen set. A network error or incomplete checkpoint stops further requests, while results from previously completed batches remain saved.

This is **read-only catalog discovery**, not automatic identity approval or metadata import. A cached Strong label still does not authorize an import. Native batch imports continue to require explicitly confirmed identities, exact live field preflight, itemized approval, and the existing journaled writer. Classic remains a temporary fallback pending full feature parity.

### Verification
The builder runs all inherited Design C source tests, exact-version checks, Windows Release build and C# unit tests in a temporary checkout before changing the real repository. A built `.pext` still requires Playnite runtime testing for full-scan cancellation, persisted cache reuse, and long-running network failures. This release does not rewrite the matching algorithm, journal formats, or importer.
