# Supabase Audit

Audit date: `2026-04-22`

This document records the live Supabase state that was verified against the current Eco-Matic repo.

## Live Schema Snapshot

Verified live public tables:

- `customers`
- `esp32_commands`
- `esp32_telemetry`
- `event_logs`
- `items`
- `machine_inventory`
- `roles`
- `sales_transactions`
- `users`
- `vending_machines`

Observed live row counts during the audit:

- `roles`: `3`
- `users`: `1`
- all other audited tables: `0`

Live schema drift found at the start of the audit:

- `public.machine_inventory` was missing `slot_price`
- `public.sales_transactions` was missing `client_sync_id`
- `public.event_logs` was missing `client_sync_id`

Those gaps were fixed during this audit pass.

Current live migrations after remediation:

1. `20260419131253 create_ecomatic_schema`
2. `20260419131307 enable_rls_policies`
3. `20260422095901 add_slot_price_and_normalize_slot_ids`
4. `20260422095906 add_client_sync_id_for_offline_replay`

## Required Live Migrations

Live schema alignment completed in this pass by applying:

1. `docs/migration_increment3.sql`
2. `docs/migration_increment4.sql`

What each migration does:

- `migration_increment3.sql`: adds `machine_inventory.slot_price` and normalizes legacy `S1` slot IDs into canonical numeric strings
- `migration_increment4.sql`: adds nullable `client_sync_id` columns plus unique partial indexes for offline replay deduplication on `sales_transactions` and `event_logs`

## Authentication Reality

The app does **not** currently use Supabase Auth.

Current authentication model:

- admin login reads from the custom `users` table
- RFID customer registration and lookup use the custom `customers` table
- both flows still store and compare raw passwords in fields named `password_hash`

Relevant code paths:

- `SupabaseStore.AuthenticateUser()`
- `SupabaseStore.AddUser()`
- `SupabaseStore_Customers.RegisterCustomer()`

This means:

- there is no session-backed identity model from Supabase Auth
- there is no password hashing yet
- login currently depends on direct password equality filtering against PostgREST

## RLS Audit Result

RLS is enabled on the audited public tables, but it is not effectively protecting data right now.

The live database currently has an anon policy equivalent to `Allow all for anon` on every audited public table, with permissive `USING (true)` and `WITH CHECK (true)` behavior for `ALL`.

Tables affected by that finding:

- `customers`
- `esp32_commands`
- `esp32_telemetry`
- `event_logs`
- `items`
- `machine_inventory`
- `roles`
- `sales_transactions`
- `users`
- `vending_machines`

Important blocker:

- the current desktop app uses the anon key to directly read and write most of these tables
- safely tightening RLS would require either a trusted backend path or a different auth architecture
- because of that, this repo documents the blocker instead of applying partial policy changes that would break the app

## Advisor Findings

Security advisor findings:

- overly permissive anon RLS policies on the audited public tables
- remediation reference: <https://supabase.com/docs/guides/database/database-linter?lint=0024_permissive_rls_policy>

Performance advisor findings:

- missing covering indexes on several foreign keys, including `event_logs.machine_id`, `machine_inventory.item_id`, `sales_transactions.machine_id`, `sales_transactions.item_id`, `users.assigned_machine_id`, and `users.role_id`
- several existing indexes are currently unused in the live project, which is unsurprising while the live tables are mostly empty
- remediation reference: <https://supabase.com/docs/guides/database/database-linter?lint=0001_unindexed_foreign_keys>

## Practical Conclusion

What is working:

- the table set exists
- the core foreign keys exist
- the repo now matches the live schema for `slot_price` and offline replay `client_sync_id`

What is still incomplete or risky:

- authentication is still custom-table auth with plain-text password handling
- RLS is effectively open to anon because of the current direct-client architecture
