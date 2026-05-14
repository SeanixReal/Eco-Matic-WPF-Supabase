# SQL Reference Layout

SQL files are grouped here so the active Supabase path is easy to review.

## Current Active Files

- `migrations/supabase/migration_increment3.sql`
- `migrations/supabase/migration_increment4.sql`
- `migrations/supabase/migration_increment5.sql`
- `migrations/supabase/migration_increment6.sql`
- `migrations/supabase/migration_increment7.sql`
- `migrations/supabase/migration_increment8_qr_payments.sql`
- `migrations/supabase/migration_increment9_user_machine_assignments.sql`
- `migrations/supabase/migration_increment10_catalog_soft_delete.sql`
- `seeds/seed_inventory.sql`

## Why There Are Multiple `migration_increment` Files

The repo accumulated incremental Supabase/PostgreSQL patches over time instead of one single reset script.

- `migration_increment3.sql` is the active Supabase/Postgres patch for `slot_price` and slot normalization
- `migration_increment4.sql` adds nullable `client_sync_id` columns for idempotent app writes
- `migration_increment5.sql` adds receipt session persistence tables
- `migration_increment6.sql` adds richer machine location fields
- `migration_increment7.sql` adds the admin-managed recycle catalog and richer recycle receipt line fields
- `migration_increment8_qr_payments.sql` adds QR payment intent storage
- `migration_increment9_user_machine_assignments.sql` adds multi-machine assignment support for inventory managers
- `migration_increment10_catalog_soft_delete.sql` adds history-safe soft delete fields and active-name indexing for the global item catalog

Only the files under `migrations/supabase/` are part of the current live Supabase migration path.
