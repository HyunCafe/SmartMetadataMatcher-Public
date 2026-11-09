# Repository consolidation

The existing public repository remains the canonical project. Its README, screenshots, license, manifests and current matching implementation are retained. Private-only development tools and records are restored; the former development README and full changelog are preserved under DevelopmentHistory. Historical readiness statements describe their original versions, not current release approval.

Both Git histories and the earlier README branch are retained as merge parents without rewriting original commits. The uploaded histories contain 51 distinct commits. A targeted scan of 604 reachable blobs, commit metadata and the annotated tag found no matching embedded token/private-key/credential literals. Personal-path matches examined were synthetic test fixtures or detection expressions. Author addresses are GitHub noreply addresses. This is not a comprehensive security certification.

Matching source and test trees are identical between the latest private and public fix branches. Their Windows test/build success was supplied by the maintainer. No installed plugin is changed by this consolidation. Historical private tooling is retained for continuity and may reference earlier readiness assumptions.

The maintainer’s GitHub inventory found no private releases, issues or pull requests. The existing public beta release and its assets remain in place. Repository settings, permissions, Projects and wiki content are not part of Git history; the private repository remains intact as an archive source. Future development belongs in the existing public repository.
