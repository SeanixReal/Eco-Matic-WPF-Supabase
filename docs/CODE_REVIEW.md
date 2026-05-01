# Eco-Matic Code Review

This document summarizes the current review of the codebase, especially the interaction between the WPF frontend, the Supabase backend, and the Arduino integration.

## Review Scope

Reviewed areas:

- app startup and mode routing
- customer vending flow
- admin inventory and reporting flow
- Supabase data access layer
- Supabase-only customer session data path
- RFID customer flow
- current documentation accuracy
- live Supabase schema and advisor findings

Build status:

- `dotnet build` succeeds with `0` warnings and `0` errors when the running `Eco-Matic` process is closed first

## Overall Verdict

The architecture is understandable and workable for a student project:

- the frontend is clearly separated from the service layer
- `SupabaseStore` centralizes backend access
- `DataStore` gives the customer UI a simple in-memory session model
- `ArduinoService` is cleanly isolated from the WPF windows

However, there are still important correctness and security gaps that should be treated as known limitations.

## Current Status After Core Inventory Refactor

The following core logic issues are now addressed:

- strict 12-slot validation is enforced for machine inventory writes
- customer slot mapping uses the real normalized `slot_id`
- the inventory model is split between a global `items` catalog and machine-specific `machine_inventory`
- machine inventory supports optional machine-item price override stored in `slot_price`; edits are propagated to every matching item slot in the selected machine
- images remain local-first for reliable classroom/demo behavior

## Highest-Priority Remaining Findings

### 1. Authentication and customer credentials are stored and checked as plain text

The code uses the field name `password_hash`, but the value is stored and queried as the raw password.

Why this matters:

- this is a real security risk
- the current docs must not describe the system as using hashed credentials
- the backend and desktop client both assume direct password equality

Relevant code:

- `Data/SupabaseStore.cs`
- `Data/SupabaseStore_Customers.cs`

Recommended next fix:

- hash passwords before storage
- compare using a proper verification flow instead of `password_hash=eq.<password>`

### 2. RLS is enabled but effectively open to anon

The audited live Supabase project currently has permissive anon policies equivalent to `Allow all for anon` across the public tables used by the app.

Why this matters:

- row-level security is not effectively protecting the current data model
- the anon key can still read and write the audited application tables
- this is a real security issue even though the tables technically have RLS enabled

Important blocker:

- the desktop app directly uses the anon key for most reads and writes
- that means safe least-privilege RLS tightening would require a larger backend/auth redesign

Recommended next fix:

- document the blocker clearly now
- defer real policy tightening until the app stops depending on direct anon table access

### 3. Runtime configuration now depends on a local `.env` file

The current client now reads Supabase settings from a repo-root `.env` file at startup.

Why this matters:

- classroom/demo setup is clearer and does not require recompiling to switch endpoints
- startup failures now surface missing config immediately
- the previously exposed anon key should still be treated as leaked and rotated in Supabase

Relevant code:

- `Data/AppEnvironment.cs`
- `Data/SupabaseClient.cs`

Current expectation:

- keep `.env.example` tracked as the setup template
- keep real `.env` ignored and local-only
- rotate the current anon key because it was already committed in earlier history

### 4. Foreign-key covering indexes are now applied

The live Supabase performance advisor previously reported missing covering indexes for:

- `event_logs.machine_id`
- `machine_inventory.item_id`
- `receipt_session_lines.recycle_item_id`
- `sales_transactions.machine_id`
- `sales_transactions.item_id`
- `users.assigned_machine_id`
- `users.role_id`

Why this matters:

- current live row counts are small, so the app still works fine in class/demo conditions
- those joins will become slower as the machine, inventory, sales, and user tables grow
- the `add_missing_foreign_key_indexes` Supabase migration added indexes for these existing columns without adding or removing tables

Current status:

- the unindexed-foreign-key advisor findings are resolved
- new indexes may show as `unused_index` until the live app runs enough queries for Supabase to observe usage

### 5. Receipt and audit data are now exercised

The live project now has real rows in the operational history tables:

- `receipt_sessions`
- `receipt_session_lines`
- `sales_transactions`
- `event_logs`
- `qr_payment_intents`

Why this matters:

- dashboards and sales reports should be checked against real mixed-date data, not only seed assumptions
- event-log descriptions now carry audit context for restocks, slot changes, customer credit changes, and RFID account history
- customers are still not linked to sales by a foreign key, so customer account history remains an application-level event-log view
- current-session purchases are attached to the first RFID used for that vending session before the dashboard loads, and a later different RFID cannot take over that session's transaction history

Recommended next fix:

- if true per-customer purchase history is required later, add a tracked migration that introduces an explicit customer or RFID reference instead of parsing audit descriptions

## Secondary Findings

### 5. Some historical docs previously overstated current behavior

Examples that needed correction:

- a previous local database path was described as active even though the code now uses Supabase
- eco-credit behavior needed clarification: purchases can use cash/QR balance or point payment, while RFID links saved credits and purchase-history attribution
- inventory was described as strictly 12 items before the service layer enforced that limit

This review pass updated the main docs to reflect the actual implementation.

## Frontend-to-Backend Interaction Summary

## Customer mode

1. `MainWindow` opens `MachineSelectionWindow`
2. `DataStore.Initialize(machineId)` loads inventory through `SupabaseSessionCoordinator`
3. `CustomerWindow` renders its 12-slot UI from `DataStore.Products`
4. when a purchase happens:
   - stock is reduced in memory
   - `DataStore.SaveInventory()` updates Supabase stock
   - `DataStore.LogEvent()` writes a Supabase event log
   - `DataStore.RecordSale()` writes a Supabase sales record
5. price shown to the customer comes from the machine item override when present, otherwise the global item default

## RFID mode

1. `ArduinoService` raises `OnCardScanned`
2. `MainWindow` checks `CustomerExists(rfid)` on a background task
3. `MainWindow` sends `VALID` or `INVALID` back to Arduino before opening dashboard/registration UI
4. registration or dashboard flow opens on the UI thread
5. pending recycle points are saved into the `customers` table from a background task

RFID implementation rule:

- do not perform Supabase/customer lookups or registration writes directly on the WPF UI thread
- do not open modal windows before the Arduino receives `VALID` or `INVALID`
- registration/dashboard modals should show busy state and disable buttons while background customer writes are running

## Admin mode

1. `LoginWindow` captures credentials
2. `SupabaseStore.AuthenticateUserAccess()` returns role and assigned machine IDs
3. `AdminWindow` enables views according to role
4. global item editing happens in the `Items` tab
5. per-machine slot assignment happens in the `Inventory` tab
6. grids are populated from `SupabaseStore` methods returning `DataTable`

## What Is Good About the Current Design

- service-layer separation is clear
- the main frontend windows are understandable
- the codebase is small enough to demo and explain well
- the database shape supports multi-machine growth
- machine registration no longer forces immediate stock setup, so physical machine setup and inventory assignment are cleaner separate workflows
- the hardware integration is isolated enough to discuss cleanly during presentation

## Best Next Improvements

1. replace plain-text passwords with real hashing
2. rotate the exposed Supabase anon key and keep `.env` local-only
3. add a database-side uniqueness/slot-range constraint to complement the service-layer checks
4. redesign backend access before attempting real least-privilege RLS tightening
5. add automated smoke tests for inventory, reporting, RFID history, and staff assignment flows
