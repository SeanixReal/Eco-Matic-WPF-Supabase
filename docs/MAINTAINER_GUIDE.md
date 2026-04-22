# Eco-Matic Maintainer Guide

This guide is for you as the project owner and maintainer.

## What This Project Is Right Now

Eco-Matic is a WPF desktop system with:

- customer vending mode
- admin management mode
- RFID-based customer registration and recycle-credit saving
- Supabase as the active backend
- Arduino serial communication for RFID and status display

## Source of Truth

For current architecture and review status, use:

- `docs/CODEBASE_ARCHITECTURE.md`
- `docs/CODE_REVIEW.md`
- `docs/DIAGRAMS.md`
- `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`

## Core Runtime Flow

### Customer flow

- machine is selected in `MachineSelectionWindow`
- inventory is loaded into `DataStore`
- `CustomerWindow` runs the vending session
- stock, logs, and sales are pushed through `SupabaseStore`

### Admin flow

- credentials go through `LoginWindow`
- `SupabaseStore.AuthenticateUser()` decides role and machine access
- `AdminWindow` loads dashboard, inventory, global items, logs, sales, machines, users, and customers

### RFID flow

- Arduino scans RFID
- `MainWindow` checks whether the customer exists
- the app opens registration or customer dashboard
- pending recycle points are saved into the customer account

## Important Invariants to Protect

- the customer UI only has 12 visible product slots
- `machine_inventory` should stay aligned with those 12 visible slots
- shared item identity belongs in `items`
- machine-specific stock and optional price override belong in `machine_inventory`
- current purchase logic is cash-based
- RFID is currently for identity and recycle-credit saving, not item payment
- `DataStore` is the in-memory customer session state, so changes there affect the vending UX directly
- customer mode now depends on the local MySQL cache and sync queue for offline resilience
- admin mode and RFID persistence still require internet access

## Things To Be Careful About Before Demoing

- make sure the selected machine does not exceed 12 active inventory entries
- make sure slot IDs are kept consistent and simple
- make sure the live Supabase project has already applied `migration_increment3.sql` and `migration_increment4.sql`
- make sure the COM port for Arduino matches the machine you are using
- make sure images referenced in the database actually exist in runtime-accessible paths

## Highest-Risk Technical Debt

- passwords are currently stored and compared directly
- Supabase URL and anon key are still hardcoded in source
- runtime behavior depends on the database having the new `slot_price` column
- offline customer mode depends on the local MySQL cache being reachable on the demo laptop
- live Supabase anon policies are currently too permissive, but safe tightening is blocked by the current direct-client architecture

## Recommended Improvement Order

1. hash passwords for users and customers
2. externalize Supabase URL and key into configuration
3. harden migration/runtime checks for databases missing `slot_price`
4. add automated tests around inventory validation and machine independence
5. consider transactional backend/RPC helpers for multi-step writes
6. expand offline sync support beyond customer mode if the product needs it later
7. redesign backend/auth before attempting strict RLS on live Supabase tables

## If You Ask AI To Help

Tell the AI these facts up front:

- backend is Supabase, not MySQL
- docs live in `docs/`
- `DataStore` is the customer session cache
- the customer UI currently has only 12 slots
- the app uses a global `items` catalog plus per-machine `machine_inventory`
- images are local-first, not Supabase Storage-first
- review findings are in `docs/CODE_REVIEW.md`
