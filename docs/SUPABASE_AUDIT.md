# Supabase Audit

Audit date: `2026-04-23`

This document records the live Supabase state verified through Supabase MCP against:

- project URL: `https://woyadcahjkutrowkzryv.supabase.co`

## Live Schema Snapshot

Verified live public tables:

- `customers`
- `esp32_commands`
- `esp32_telemetry`
- `event_logs`
- `items`
- `machine_inventory`
- `receipt_session_lines`
- `receipt_sessions`
- `roles`
- `sales_transactions`
- `users`
- `vending_machines`

Observed live row counts during this audit:

- `items`: `12`
- `machine_inventory`: `24`
- `roles`: `3`
- `users`: `1`
- `vending_machines`: `2`
- `customers`: `0`
- `esp32_commands`: `0`
- `esp32_telemetry`: `0`
- `event_logs`: `0`
- `receipt_session_lines`: `0`
- `receipt_sessions`: `0`
- `sales_transactions`: `0`

Live machine data observed:

- machine `5`: `IT Park Green Hub`
- machine `6`: `Fuente Eco Stop`
- both machines are `Active`
- both machines currently have a human-readable address plus latitude and longitude
- both machines currently have all `12` canonical slots populated
- current `slot_id` values are normalized as `1` through `12`
- all current machine inventory rows have a non-null `slot_price`

## Live Migrations

Current live migrations reported by Supabase MCP:

1. `20260419131253 create_ecomatic_schema`
2. `20260419131307 enable_rls_policies`
3. `20260422095901 add_slot_price_and_normalize_slot_ids`
4. `20260422095906 add_client_sync_id_for_app_idempotency`
5. `20260422151624 add_receipt_session_history`
6. `20260423085816 add_vending_machine_address_and_coordinates`

Practical interpretation:

- the live project now includes the slot-price refactor
- the live project now includes `client_sync_id` idempotency columns for sales and event logs
- the live project now includes receipt session history tables
- the live project now includes machine address and map-coordinate columns

No schema drift was found in those audited areas relative to the current repo migrations.

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

- there is no Supabase Auth session model in the current desktop app
- there is no password hashing yet
- login still depends on direct password equality filtering through PostgREST

## RLS Audit Result

RLS is enabled on the audited public tables, but it is not effectively protecting data right now.

Supabase advisor findings show an anon policy equivalent to `Allow all for anon` on every audited application table, with permissive `USING (true)` and `WITH CHECK (true)` behavior for `ALL`.

Tables affected by that finding:

- `customers`
- `esp32_commands`
- `esp32_telemetry`
- `event_logs`
- `items`
- `machine_inventory`
- `receipt_session_lines`
- `receipt_sessions`
- `roles`
- `sales_transactions`
- `users`
- `vending_machines`

Important blocker:

- the current desktop app directly uses the anon key for most reads and writes
- safe least-privilege RLS tightening would require a backend or auth redesign
- tightening those policies in isolation would break the current app

## Advisor Findings

Security advisor findings:

- overly permissive anon RLS policies across the public application tables
- remediation reference: <https://supabase.com/docs/guides/database/database-linter?lint=0024_permissive_rls_policy>

Performance advisor findings:

- missing covering indexes on `event_logs.machine_id`
- missing covering indexes on `machine_inventory.item_id`
- missing covering indexes on `sales_transactions.machine_id`
- missing covering indexes on `sales_transactions.item_id`
- missing covering indexes on `users.assigned_machine_id`
- missing covering indexes on `users.role_id`
- `idx_receipt_session_lines_session_order` is currently unused, which is unsurprising while receipt tables are empty
- remediation reference: <https://supabase.com/docs/guides/database/database-linter?lint=0001_unindexed_foreign_keys>

## Practical Conclusion

What is working:

- the live table set matches the repo for the audited application features
- the live project contains the slot-price, client-sync-id, receipt-session, and machine-location migrations
- current live inventory data is aligned with the 12-slot customer UI

What is still incomplete or risky:

- authentication is still custom-table auth with plain-text password handling
- RLS is effectively open to anon because of the current direct-client architecture
- live activity tables are mostly empty, so receipt history and ESP32 integrations have schema support but very little production data coverage yet
- several foreign keys still need supporting indexes
