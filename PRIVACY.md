# Privacy and data handling

## Network connections

When the user invokes IGDB operations, the plugin contacts:

- `https://id.twitch.tv` — OAuth access token request using the user's Client ID/Secret.
- `https://api.igdb.com` — IGDB game lookup requests based on Playnite titles, platform and selected IGDB identities.
- `https://images.igdb.com` — optional IGDB cover/icon/screenshot retrieval.

The service providers can receive IP/network information and game lookup terms; refer to Twitch/IGDB privacy terms for their processing. No independent analytics or telemetry endpoint is intended.

## Local data

Credentials are stored in extension-owned local user data encrypted using Windows CurrentUser DPAPI; in-memory access tokens are transient. The plugin maintains local catalog scan cache, confirmations, exclusions, operation journals and optional diagnostic exports. Exported CSVs can reveal the user's owned games and gaming preferences; inspect and redact them before sharing publicly.

## Files and game state

Catalog scans are read-only to the game library. Approved imports may write supported Playnite metadata and media references through Playnite; they do not modify ROM contents or launch paths. Local staging and undo files preserve sufficient state to support recoverable changes, but a separate backup of both Playnite library and plugin data is still recommended.

## Removal

Uninstalling the extension does not guarantee deletion of its per-plugin data or undo history. Review Playnite's extension user-data directories before removing any data, and keep a backup until recovery is no longer needed.
