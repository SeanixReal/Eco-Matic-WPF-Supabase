# Eco-Matic Maintainer Guide

This guide is for you as the project owner and maintainer.

## What This Project Is Right Now

Eco-Matic is a WPF desktop system with:

- customer vending mode
- admin management mode
- RFID-based customer registration and recycle-credit saving
- Supabase as the active backend
- Arduino serial communication for RFID and status display
- map-assisted vending machine address capture with manual override

## Source of Truth

For current architecture and review status, use:

- `docs/CODEBASE_ARCHITECTURE.md`
- `docs/CODE_REVIEW.md`
- `docs/DIAGRAMS.md`
- `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`

## Core Runtime Flow

### Customer flow

- machine is selected in `MachineSelectionWindow`
- machine cards now show both machine name and address when available
- inventory is loaded into `DataStore`
- `CustomerWindow` runs the vending session
- `ReceiptWindow` now shows machine name and address on-screen as well as in the print path
- stock, logs, and sales are pushed through `SupabaseStore`

### Admin flow

- credentials go through `LoginWindow`
- `SupabaseStore.AuthenticateUserAccess()` decides role and machine access
- admin users open on the Dashboard view after successful login
- `AdminWindow` loads dashboard, inventory, global items, logs, sales, machines, users, and customers
- inventory managers only load the Inventory page for their assigned vending machines
- global item add/edit checks the current catalog grid for duplicate item names and shows a clear warning before calling Supabase
- the Inventory page supports quantity restocking and a selected-item action to restock directly to max capacity
- newly registered vending machines are allowed to start with zero assigned inventory slots; admins assign stock afterward from the Inventory view
- the Sales Report defaults to Week and can filter all machines or one vending machine, using the selected date as the anchor for Day, Week, Month, or Year
- analytics now include revenue trend, product mix, best-selling items, machine revenue, category revenue, peak sales periods, top machine, transaction count, unit count, and average sale for the active filter
- the Event Logs page also defaults to Week so recent operational activity is visible without switching filters
- the staff editor keeps Register/Cancel outside the scrolling machine list so assignments can grow without hiding the action buttons
- restocks, slot assignment/removal/update, stock changes, catalog edits, machine edits, customer registration, and credit updates are written to `event_logs`
- machine setup/edit now stores a machine name, an editable address, and optional coordinates
- `MapPickerWindow` plus `MapLocationService` auto-fill an address from a clicked map point

### RFID flow

- Arduino scans RFID
- `MainWindow` checks whether the customer exists
- the app opens registration or customer dashboard
- pending recycle points are saved into the customer account
- before the dashboard opens, the active `CustomerWindow` attaches any unlinked current-session purchases to the first valid RFID for that session
- after a session has purchases attached to one RFID, a different RFID can view its dashboard but cannot take over the active session's transaction history or pending point save
- the dashboard shows recent transaction history by filtering purchase-related `event_logs` for the exact scanned RFID value, plus current-session rows that may still be writing in the background

## Important Invariants to Protect

- the customer UI only has 12 visible product slots
- `machine_inventory` should stay aligned with those 12 visible slots
- shared item identity belongs in `items`
- machine-specific stock and optional price override belong in `machine_inventory`
- machine identity belongs in `vending_machines.location_name`
- machine address and coordinates belong in `vending_machines.address_text`, `latitude`, and `longitude`
- purchases can use inserted cash/QR-paid balance or eco-points when point payment is toggled
- RFID is currently for identity, recycle-credit saving, point payment, and purchase-history attribution
- `DataStore` is the in-memory customer session state, so changes there affect the vending UX directly
- customer account transaction history is purchase-log based because the current schema does not foreign-key customers to sales records
- customer mode now depends on the local MySQL cache and sync queue for offline resilience
- admin mode and RFID persistence still require internet access

## Things To Be Careful About Before Demoing

- make sure the selected machine does not exceed 12 active inventory entries
- make sure slot IDs are kept consistent and simple
- make sure the live Supabase project has already applied `sql/migrations/supabase/migration_increment3.sql` and `sql/migrations/supabase/migration_increment4.sql`
- make sure the live Supabase project has also applied `sql/migrations/supabase/migration_increment5.sql`
- make sure the live Supabase project has also applied `sql/migrations/supabase/migration_increment6.sql`
- make sure the COM port for Arduino matches the machine you are using
- make sure images referenced in the database actually exist in runtime-accessible paths
- make sure each demo machine has a readable machine name and address because both are now surfaced to the customer and available on receipts
- make sure any newly registered demo machine has inventory assigned before opening it in Customer Mode; registration itself no longer forces initial stock
- make sure the live `roles` table has `Admin` and `Inventory Manager`; staff creation expects `Inventory Manager`
- make sure `user_machine_assignments` exists when using multiple assigned vending machines
- demo sales rows use deterministic `client_sync_id` values and can be reseeded without stacking duplicates
- Supabase migration `add_missing_foreign_key_indexes` adds indexes on existing foreign-key columns only; it does not add or remove tables

## Highest-Risk Technical Debt

- passwords are currently stored and compared directly
- the Supabase anon key was previously exposed in source/history and should still be rotated in Supabase
- startup now depends on a valid repo-root `.env` file for both Supabase and local MySQL settings
- runtime behavior depends on the database having the new `slot_price` column
- offline customer mode depends on the local MySQL cache being reachable on the demo laptop
- map picking depends on WebView2 and internet access for reverse geocoding, although the address can still be typed manually
- live Supabase anon policies are currently too permissive, but safe tightening is blocked by the current direct-client architecture

## Recommended Improvement Order

1. hash passwords for users and customers
2. rotate the exposed Supabase anon key and keep `.env` local-only
3. harden migration/runtime checks for databases missing `slot_price`
4. add automated tests around inventory validation and machine independence
5. consider transactional backend/RPC helpers for multi-step writes
6. expand offline sync support beyond customer mode if the product needs it later
7. redesign backend/auth before attempting strict RLS on live Supabase tables
8. monitor Supabase advisor results after demo traffic; newly created indexes can appear unused until queries hit them

## If You Ask AI To Help

Tell the AI these facts up front:

- backend is Supabase, not MySQL
- docs live in `docs/`
- `DataStore` is the customer session cache
- the customer UI currently has only 12 slots
- the app uses a global `items` catalog plus per-machine `machine_inventory`
- inventory managers are assigned to one or more machines and only manage those machines' inventory
- sales reports support an all-machine view and a single-machine filter
- machine setup now includes machine name, editable address, and optional map coordinates
- images are local-first, not Supabase Storage-first
- review findings are in `docs/CODE_REVIEW.md`
