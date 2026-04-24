# Supabase MCP Analysis

Analysis date: `2026-04-23`

This note captures what was directly verified through Supabase MCP before updating the project docs.

## MCP Checks Used

- `list_tables(public, verbose=true)`
- `list_migrations()`
- `get_project_url()`
- `get_advisors(security)`
- `get_advisors(performance)`
- targeted `execute_sql(...)` queries for machine rows, slot coverage, and table counts

## Verified Live Data Facts

- the connected project is `https://woyadcahjkutrowkzryv.supabase.co`
- the live public schema contains `12` application tables
- `items` currently contains `12` rows
- `vending_machines` currently contains `2` active rows
- `machine_inventory` currently contains `24` rows, which means `12` assigned slots per machine
- current machine inventory slot IDs are normalized as `1` through `12` for both live machines
- `receipt_sessions`, `receipt_session_lines`, `sales_transactions`, `event_logs`, `customers`, `esp32_commands`, and `esp32_telemetry` are currently empty
- `roles` currently contains `Admin`, `Operator`, and `Viewer`

## Doc Impact

The MCP findings required these documentation corrections:

- add `receipt_sessions` and `receipt_session_lines` to the live schema docs
- add `esp32_commands` and `esp32_telemetry` to the live schema docs
- update migration history to include receipt-session and machine-location migrations
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
