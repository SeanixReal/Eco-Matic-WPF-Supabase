# Supabase MCP Analysis

Analysis date: `2026-04-30`

This note captures what was directly verified through Supabase MCP before updating the project docs.

## MCP Checks Used

- `list_tables(public, verbose=true)`
- `list_migrations()`
- `get_project_url()`
- `get_advisors(security)`
- `get_advisors(performance)`
- read-only `list_tables(public, verbose=true)` verification for live columns, relationships, and row counts

## Verified Live Data Facts

- the connected project is `https://woyadcahjkutrowkzryv.supabase.co`
- the live public schema contains `13` application tables
- `items` currently contains `12` rows
- `vending_machines` currently contains `4` rows
- `machine_inventory` currently contains `37` rows
- `sales_transactions` currently contains `928` rows
- `event_logs` currently contains `91` rows
- `customers` currently contains `1` row
- `receipt_sessions` currently contains `120` rows
- `receipt_session_lines` currently contains `260` rows
- `recyclable_items` currently contains `6` rows
- `qr_payment_intents` currently contains `37` rows
- `roles` currently contains `Admin` and `Inventory Manager`
- `user_machine_assignments` stores multi-machine inventory-manager scope
- migration `20260429184116_add_missing_foreign_key_indexes` adds covering indexes for the advisor-reported foreign keys on existing tables only

## Doc Impact

The MCP findings required these documentation corrections:

- add `receipt_sessions` and `receipt_session_lines` to the live schema docs
- remove ESP32 telemetry/command tables from the live ERD until those tables exist in the connected Supabase schema
- document audit-log-based customer account history because there is no customer foreign key on sales records
- update migration history to include receipt-session and machine-location migrations
- document that the foreign-key index migration improves retrieval/delete performance without changing table relationships
- correct the professor guide, which previously said offline sync was not supported yet
- correct the ERD, which previously showed columns not present in the live schema

## Important Interpretation

Two things are true at the same time:

- the project is Supabase-backed for its main backend
- the current codebase still uses a local MySQL store for customer-mode offline cache and replay

So the accurate phrasing is:

- `MySqlStore` as the old primary backend is historical
- `OfflineMySqlStore` as a local cache is still an active part of the current runtime architecture

## Recommended Next Review Habit

When updating architecture or deployment docs again, re-run the same MCP checks first so the docs keep reflecting the live project instead of only the local repo assumptions.
