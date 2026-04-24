# Eco-Matic Code Review

This document summarizes the current review of the codebase, especially the interaction between the WPF frontend, the Supabase backend, the local offline cache, and the Arduino integration.

## Review Scope

Reviewed areas:

- app startup and mode routing
- customer vending flow
- admin inventory and reporting flow
- Supabase data access layer
- offline customer cache and replay path
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
- machine inventory supports optional per-slot price override
- images remain local-first for reliable offline/demo behavior

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

The current client now reads both Supabase and local MySQL settings from a repo-root `.env` file at startup.

Why this matters:

- classroom/demo setup is clearer and does not require recompiling to switch endpoints
- startup failures now surface missing config immediately instead of failing later in the offline bootstrap path
- the previously exposed anon key should still be treated as leaked and rotated in Supabase

Relevant code:

- `Data/AppEnvironment.cs`
- `Data/SupabaseClient.cs`

Current expectation:

- keep `.env.example` tracked as the setup template
- keep real `.env` ignored and local-only
- rotate the current anon key because it was already committed in earlier history

### 4. Several foreign keys are still missing covering indexes

The live Supabase performance advisor currently reports missing covering indexes for:

- `event_logs.machine_id`
- `machine_inventory.item_id`
- `sales_transactions.machine_id`
- `sales_transactions.item_id`
- `users.assigned_machine_id`
- `users.role_id`

Why this matters:

- current live row counts are small, so the app still works fine in class/demo conditions
- those joins will become slower as the machine, inventory, sales, and user tables grow
- the missing indexes are now a concrete live-database maintenance task, not just a theoretical optimization

Recommended next fix:

- add indexes in a tracked Supabase migration so future environments stay aligned

### 5. Schema surface is ahead of real data coverage

The live project now includes:

- `receipt_sessions`
- `receipt_session_lines`
- `esp32_commands`
- `esp32_telemetry`

But those tables are currently empty in the audited environment.

Why this matters:

- the schema is ready for receipt history and ESP32 integration
- documentation should mention these tables because they are real
- the app still needs more exercised runtime data and testing around those paths

## Secondary Findings

### 5. Some historical docs previously overstated current behavior

Examples that needed correction:

- MySQL was described as the active backend even though the code uses Supabase
- eco-credits were described as a payment method even though purchases are still cash-based
- inventory was described as strictly 12 items before the service layer enforced that limit

This review pass updated the main docs to reflect the actual implementation.

## Frontend-to-Backend Interaction Summary

## Customer mode

1. `MainWindow` opens `MachineSelectionWindow`
2. `DataStore.Initialize(machineId)` loads inventory from the local offline cache
3. `CustomerWindow` renders its 12-slot UI from `DataStore.Products`
4. when a purchase happens:
   - stock is reduced in memory
   - `DataStore.SaveInventory()` updates the local cache and marks stock for replay
   - `DataStore.LogEvent()` queues an event log for replay
   - `DataStore.RecordSale()` queues a sales record for replay
5. price shown to the customer comes from the machine slot override when present, otherwise the global item default

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
2. `SupabaseStore.AuthenticateUser()` returns role and assigned machine
3. `AdminWindow` enables views according to role
4. global item editing happens in the `Items` tab
5. per-machine slot assignment happens in the `Inventory` tab
6. grids are populated from `SupabaseStore` methods returning `DataTable`

## What Is Good About the Current Design

- service-layer separation is clear
- the main frontend windows are understandable
- the codebase is small enough to demo and explain well
- the database shape supports multi-machine growth
- the hardware integration is isolated enough to discuss cleanly during presentation

## Best Next Improvements

1. replace plain-text passwords with real hashing
2. rotate the exposed Supabase anon key and keep `.env` local-only
3. add Supabase indexes for the currently unindexed foreign keys
4. redesign backend access before attempting real least-privilege RLS tightening
5. add a database-side uniqueness/slot-range constraint to complement the service-layer checks
