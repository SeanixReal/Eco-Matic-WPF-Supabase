# SQL Reference Layout

SQL files are grouped here so the repo is easier to scan and the active Supabase path is separated from old MySQL-era reference files.

## Current Active Files

- `migrations/supabase/migration_increment3.sql`
- `migrations/supabase/migration_increment4.sql`
- `migrations/supabase/migration_increment5.sql`
- `migrations/supabase/migration_increment6.sql`
- `migrations/supabase/migration_increment7.sql`
- `migrations/supabase/migration_increment8_qr_payments.sql`
- `migrations/supabase/migration_increment9_user_machine_assignments.sql`
- `seeds/seed_inventory.sql`

## Historical Files

- `archive/mysql/database_setup.sql`
- `archive/mysql/migration_increment2.sql`

## Why There Are Multiple `migration_increment` Files

The repo accumulated incremental SQL patches over time instead of one single reset script.

- `migration_increment2.sql` belongs to the older MySQL-era project phase and is kept only for historical reference
- `migration_increment3.sql` is the active Supabase/Postgres patch for `slot_price` and slot normalization
- `migration_increment4.sql` is the active Supabase/Postgres patch for offline replay idempotency
- `migration_increment5.sql` adds receipt session persistence tables
- `migration_increment6.sql` adds richer machine location fields
- `migration_increment7.sql` adds the admin-managed recycle catalog and richer recycle receipt line fields
- `migration_increment8_qr_payments.sql` adds QR payment intent storage
- `migration_increment9_user_machine_assignments.sql` adds multi-machine assignment support for inventory managers

Only the files under `migrations/supabase/` are part of the current live Supabase migration path.
