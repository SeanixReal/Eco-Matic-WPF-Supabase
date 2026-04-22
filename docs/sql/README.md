# SQL Reference Layout

SQL files are grouped here so the repo is easier to scan and the active Supabase path is separated from old MySQL-era reference files.

## Current Active Files

- `migrations/supabase/migration_increment3.sql`
- `migrations/supabase/migration_increment4.sql`
- `seeds/seed_inventory.sql`

## Historical Files

- `archive/mysql/database_setup.sql`
- `archive/mysql/migration_increment2.sql`

## Why There Are Multiple `migration_increment` Files

The repo accumulated incremental SQL patches over time instead of one single reset script.

- `migration_increment2.sql` belongs to the older MySQL-era project phase and is kept only for historical reference
- `migration_increment3.sql` is the active Supabase/Postgres patch for `slot_price` and slot normalization
- `migration_increment4.sql` is the active Supabase/Postgres patch for offline replay idempotency

Only the files under `migrations/supabase/` are part of the current live Supabase migration path.
