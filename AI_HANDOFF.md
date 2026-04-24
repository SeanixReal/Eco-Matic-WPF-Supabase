# AI Handoff

Project: `Eco-Matic-WPF-Supabase`
Path: `C:\Users\Seani\Desktop\School\2nd Year\CPE262\Project\Eco-Matic-Final\Eco-Matic-WPF-Supabase`

## Current Repo State

- Branch: `main`
- Latest pushed commits:
  - `af2ad9f` `docs: document supabase schema and online-first limits`
  - `100036a` `feat: add global catalog and per-machine slot management`
  - `e72c958` `docs: consolidate repository documentation`

## What Was Already Implemented

- Global catalog plus per-machine inventory refactor
- Separate admin `Items` flow vs machine `Inventory` flow
- Canonical slot handling using `1..12`
- 12-slot enforcement in service-layer writes
- Real slot-based customer mapping instead of row-order mapping
- Optional per-machine `slot_price` support in code
- Local-first image handling
- Documentation refresh under `docs/`

## Important Runtime Truth

- The app is **not** offline-first.
- There is **no durable local database cache** and **no sync/replay queue**.
- `DataStore` is only in-memory session state.
- If Wi-Fi drops after data is already loaded, some UI behavior may continue briefly, but durable offline sync is not implemented.

## Supabase Status

- Repo config is in `Data/SupabaseClient.cs`.
- A real Supabase anon key is hardcoded there and should be treated as exposed.
- Previous live REST check showed `machine_inventory.slot_price` does **not** yet exist in the live database.
- `docs/migration_increment3.sql` was added to handle that schema update.

## MCP Status

- Local config includes a Supabase MCP server in `C:\Users\Seani\.codex\config.toml`.
- Config entry:
  - `[mcp_servers.supabase]`
  - `url = "https://mcp.supabase.com/mcp?project_ref=woyadcahjkutrowkzryv"`
- In this session, MCP initialization failed before resources loaded.
- Error summary:
  - OAuth token refresh failed
  - Failed to parse server response
  - Handshake failed during MCP startup
- Next session should retry MCP first before assuming it is unavailable.

## User's Latest Direction

The next session should focus on:

1. Cleaning up the repo further
2. Explaining why there are multiple migration increment files
3. Explaining what `docs/seed_inventory.sql` is for
4. Checking for sensitive keys/secrets being pushed
5. Cleaning and documenting the repo accordingly

## Migration Notes

### `docs/migration_increment2.sql`

- Older migration
- Adds `dispense_message` and `examine_message`
- Changes `machine_inventory` slot/index structure
- Written in older MySQL-style migration form

### `docs/migration_increment3.sql`

- Newer migration
- Adds `slot_price`
- Normalizes legacy `S1` style slot IDs into canonical `1`

### Likely Cleanup Decision

- Keep only migrations that still matter
- Document clearly what each migration is for
- Consider renaming them to more descriptive names if the repo should be easier to maintain

## Seed File Note

`docs/seed_inventory.sql` is:

- sample seed data
- not a migration
- used to populate demo or initial global items and per-machine inventory assignments

## Sensitive Findings Already Confirmed

### Real exposure

- `Data/SupabaseClient.cs`
  - hardcoded `SUPABASE_URL`
  - hardcoded `SUPABASE_ANON_KEY`

### Not a real secret

- `Data/Esp32SupabaseClient.ino`
  - only contains a placeholder key string

## Recommended Next Actions

1. Retry Supabase MCP access in the new session
2. Audit repo config and secret handling
3. Move Supabase URL/key toward local config or environment-backed settings
4. Update `.gitignore` and add a safe config template if needed
5. Clean up migration/docs structure
6. Update documentation after cleanup
7. Run `dotnet build`
8. Commit only the intentional cleanup/hardening changes
