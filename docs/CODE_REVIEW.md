# Eco-Matic Code Review

This document summarizes the current review of the codebase, especially the interaction between the WPF frontend, the Supabase backend, and the Arduino integration.

## Review Scope

Reviewed areas:

- app startup and mode routing
- customer vending flow
- admin inventory and reporting flow
- Supabase data access layer
- RFID customer flow
- current documentation accuracy

Build status:

- `dotnet build` succeeds with `0` warnings and `0` errors

## Overall Verdict

The architecture is understandable and workable for a student project:

- the frontend is clearly separated from the service layer
- `SupabaseStore` centralizes backend access
- `DataStore` gives the customer UI a simple in-memory session model
- `ArduinoService` is cleanly isolated from the WPF windows

However, there are some important correctness and security gaps that should be treated as known limitations.

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

### 2. Supabase configuration is still hardcoded in the client

The current client still embeds the Supabase URL and anon key directly in source.

Why this matters:

- configuration rotation is harder
- it is easy for docs and environments to drift
- classroom/demo settings cannot be switched cleanly without recompiling

Relevant code:

- `Data/SupabaseClient.cs`

Recommended next fix:

- move the values into configuration or environment-backed settings
- document the expected deployment configuration clearly

## Secondary Findings

### 3. Some historical docs previously overstated current behavior

Examples that needed correction:

- MySQL was described as the active backend even though the code uses Supabase
- eco-credits were described as a payment method even though purchases are still cash-based
- inventory was described as strictly 12 items before the service layer enforced that limit

This review pass updated the main docs to reflect the actual implementation.

## Frontend-to-Backend Interaction Summary

## Customer mode

1. `MainWindow` opens `MachineSelectionWindow`
2. `DataStore.Initialize(machineId)` loads inventory from `SupabaseStore.GetMachineInventory(machineId)`
3. `CustomerWindow` renders its 12-slot UI from `DataStore.Products`
4. when a purchase happens:
   - stock is reduced in memory
   - `DataStore.SaveInventory()` updates Supabase
   - `DataStore.LogEvent()` writes an event log
   - `DataStore.RecordSale()` writes a sales record
5. price shown to the customer comes from the machine slot override when present, otherwise the global item default

## RFID mode

1. `ArduinoService` raises `OnCardScanned`
2. `MainWindow` checks `CustomerExists(rfid)`
3. registration or dashboard flow opens
4. pending recycle points are eventually saved into the `customers` table

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
2. externalize the Supabase configuration instead of hardcoding it in the client
3. apply `docs/migration_increment3.sql` to live Supabase projects that do not yet have `machine_inventory.slot_price`
4. add a database-side uniqueness/slot-range constraint to complement the service-layer checks
