# Eco-Matic Codebase Architecture

## 1. Project Summary

Eco-Matic is a WPF desktop application for a smart vending machine with an eco-credit feature. The system combines three main responsibilities:

- customer vending
- admin inventory and machine management
- RFID-based customer identification for recycling credits

The current implementation uses a layered, MVVM-lite structure. The UI logic stays in the window code-behind files, while reusable work such as database communication, serial communication, and image loading is moved into service classes.

## 2. High-Level Architecture

The codebase is easiest to understand as four cooperating layers:

### A. Presentation Layer

This is the WPF user interface.

- `MainWindow.xaml.cs`
  - acts as the entry point
  - routes users to customer mode or admin mode
  - listens for RFID scans from Arduino
- `CustomerWindow.xaml.cs`
  - handles the vending machine experience
  - manages money insertion, item selection, recycle points in the current session, and receipt flow
- `AdminWindow.xaml.cs`
  - acts as the admin control center
  - loads dashboard, inventory, global items, sales, logs, machines, users, and customer data
- supporting dialog windows
  - `LoginWindow`
  - `CatalogItemWindow`
  - `MachineSelectionWindow`
  - `MapPickerWindow`
  - `InventoryItemWindow`
  - `RestockWindow`
  - `AddMachineWindow`
  - `EditMachineWindow`
  - `UserEditorWindow`
  - `CustomerRegistrationWindow`
  - `CustomerDashboardWindow`
  - `ReceiptWindow`

### B. Application and Session Layer

This layer keeps temporary application state that is useful while a vending session is active.

- `Data/DataStore.cs`
  - stores the active machine ID
  - loads inventory from the backend into memory
  - keeps the in-session product list
  - tracks the latest transaction and pending recycle points

This is important because not every UI action needs to call the backend immediately. The customer screen works against an in-memory session model and now reads from a durable local cache before replaying queued customer writes back to Supabase.

Important limitation:

- `DataStore` is still the in-memory session layer used by the customer UI
- durable offline storage and replay are handled by the local MySQL cache plus sync queue
- admin mode and RFID customer account writes are still online-only in v1

### C. Service and Integration Layer

This layer connects the UI to external systems.

- `Data/SupabaseStore.cs`
  - main application data service
  - contains CRUD and reporting operations
  - returns `DataTable` objects because WPF `DataGrid` binding is used throughout the admin screens
- `Data/SupabaseStore_Customers.cs`
  - extends `SupabaseStore` with customer RFID operations
  - implemented as a partial class to keep customer-specific logic separate
- `Data/SupabaseClient.cs`
  - low-level REST client for Supabase PostgREST
  - performs `GET`, `POST`, `PATCH`, `DELETE`, and RPC calls
- `Data/MapLocationService.cs`
  - reverse-geocodes selected map coordinates into a readable address
  - supports map-assisted vending machine setup while keeping manual editing possible
- `Data/ArduinoService.cs`
  - handles serial communication with the Arduino
  - raises an event when an RFID card is scanned
  - sends response and LCD state commands back to the device
- `Utilities/ImageLoader.cs`
  - loads product images from pack URIs or local files
  - protects the UI from broken image paths

### D. Domain Model Layer

This layer contains the main business objects used inside the application.

- `Models/VendingItem.cs`
  - abstract base class for sellable items
- `Models/Product.cs`
  - concrete product family with inheritance
  - includes `SnackItem`, `DrinkItem`, and `MiscItem`
- `Models/Transaction.cs`
  - transaction-related classes
  - `Transaction`
  - `TransactionItem`
  - `RecycleEntry`
  - `EventLogEntry`

## 3. Main Runtime Flows

### 3.1 Customer Vending Flow

1. `MainWindow` opens `MachineSelectionWindow`.
2. The machine selection screen shows the machine name and address to help the customer identify the correct kiosk.
3. The selected machine ID is stored through `DataStore.Initialize(machineId)`.
4. `DataStore` loads the machine inventory from the local offline cache.
5. `CustomerWindow` displays the 12-slot vending layout using the in-memory `DataStore.Products` list.
6. When the customer buys an item:
   - money is validated in the UI
   - stock is decreased in memory
   - `DataStore.SaveInventory()` updates the local cache and marks dirty stock for replay
   - `DataStore.LogEvent()` queues a customer event log for replay
   - `DataStore.RecordSale()` queues a sales record for replay
   - a receipt can be shown through `ReceiptWindow`
   - the on-screen receipt can show the selected machine name and address
   - the printed receipt can include the selected machine name and address

Important current behavior:

- products are placed into customer slots using the real normalized `slot_id`
- effective price comes from the machine slot override when present, otherwise the global default item price

### 3.2 RFID and Eco-Credit Flow

1. `ArduinoService` receives serial data from the RFID scanner.
2. `MainWindow` listens to `OnCardScanned`.
3. If the RFID exists:
   - `CustomerDashboardWindow` is opened
   - pending recycle points are saved into the `customers` table
4. If the RFID does not exist:
   - `CustomerRegistrationWindow` is opened first
   - after registration, the dashboard can open

Important implementation note:

- recycle points are persisted to the customer account
- the current purchase flow is still cash-based in `CustomerWindow`
- the RFID customer account is used for registration, identification, and point saving, not for deducting payment during purchase

### 3.3 Admin Flow

1. `MainWindow` opens `LoginWindow`.
2. `SupabaseStore.AuthenticateUser()` validates the username and password and returns the role plus assigned machine.
3. `AdminWindow` loads with role-based restrictions.
4. If the role is `Inventory Manager`, machine access is limited and finance-related sections are hidden.
5. The admin window uses one shell window and switches views with `Visibility` toggling instead of opening many separate pages.
6. Machine setup now stores:
   - a machine name in `vending_machines.location_name`
   - an editable address in `vending_machines.address_text`
   - optional map-picked coordinates in `vending_machines.latitude` and `vending_machines.longitude`

Important admin split:

- the `Items` tab manages the shared global catalog in `items`
- the `Inventory` tab manages per-machine slot assignment, stock, and optional slot-specific price override in `machine_inventory`
- the `Machines` tab manages machine identity and physical placement information

## 4. Why the Architecture Looks Like This

### 4.1 Why `DataStore` Exists Even with a Database

`DataStore` acts as a session cache for the customer-facing machine. This gives three practical benefits:

- the vending UI can update immediately without re-querying the backend on every button click
- temporary state such as `PendingPoints` and `LastTransaction` can be kept in one place
- the customer flow stays simple while still syncing important records to the database

### 4.2 Why `SupabaseStore` Returns `DataTable`

The admin screens use WPF data grids heavily. Returning `DataTable` objects keeps the binding simple and reduces the amount of UI refactoring required.

### 4.3 Why `SupabaseStore` Wraps Async Calls Synchronously

The store internally calls async HTTP methods through `SupabaseClient`, but exposes synchronous methods to the rest of the app. This was likely done to fit the current code-behind WPF architecture without redesigning all event handlers.

### 4.4 Why `items` and `machine_inventory` Are Separate

This is a strong database design choice:

- `items` stores the master catalog
- `machine_inventory` stores which machine has which item in which slot, with stock, capacity, and optional machine-specific price

That separation avoids duplicated product definitions across machines.

## 5. Current Folder Map

- `Data/`
  - data access, backend communication, serial communication
- `Models/`
  - business entities and transaction models
- `Utilities/`
  - helper classes such as image loading
- root `*.xaml` and `*.xaml.cs`
  - presentation layer windows
- `Arduino/`
  - microcontroller firmware and setup notes
- `Assets/Images/`
  - product images

## 6. Persistent Data Model

The current backend revolves around these main entities:

- `roles`
- `users`
- `vending_machines`
- `items`
- `machine_inventory`
- `sales_transactions`
- `event_logs`
- `customers`
- `receipt_sessions`
- `receipt_session_lines`
- `esp32_telemetry`
- `esp32_commands`

One important detail for presentation:

- `customers` is logically related to RFID and eco-credits
- but it is not currently linked by foreign key to `sales_transactions`
- the connection is done at application level through RFID scanning and `PendingPoints`

Important vending-machine detail:

- `vending_machines.location_name` now acts as the machine name shown in admin and customer selection
- `vending_machines.address_text` stores the human-readable address
- `vending_machines.latitude` and `vending_machines.longitude` store optional map-selected coordinates

That means your ERD should show `customers` as an independent table in the current implementation.

## 7. What to Emphasize During Defense

- The project is not just a UI. It integrates desktop software, a relational backend, and external hardware.
- The design separates user interface logic from database and hardware services.
- The admin side and customer side share the same backend but solve different use cases.
- The architecture uses inheritance in the product model and event-driven programming in the Arduino integration.
- The schema supports multiple vending machines, not just one machine.

## 8. Known Documentation Mismatch in the Repository

Some older archived or non-canonical files in the repository still describe the project as MySQL-driven through `MySqlStore`.

For the current codebase, the accurate implementation is:

- `SupabaseStore`
- `SupabaseClient`
- REST access to Supabase
- optional local `OfflineMySqlStore` cache for customer-mode offline sync

Conceptually, the schema is still relational, so the ERD explanation remains valid, but the access technology has changed and the live schema now also includes receipt-history and ESP32-support tables.

For the live Supabase audit status, migrations, and current auth/RLS findings, see `docs/SUPABASE_AUDIT.md` and `docs/SUPABASE_MCP_ANALYSIS.md`.

## 9. Current Review Notes

The latest review found these implementation caveats:

- the customer UI has 12 visible slots, and the backend now enforces that limit for machine inventory
- `DataStore.Initialize()` now maps products to customer slots using the real normalized `slot_id`
- RFID is used for registration and recycle-credit saving, not for direct purchase payment
- password fields are currently stored and compared directly even though some field names still say `password_hash`
- the image strategy is local-first rather than Supabase Storage-first to keep classroom/demo behavior reliable
- customer mode now supports durable offline cache-and-replay after one successful online sync
- admin mode and RFID persistence still require live connectivity

See `docs/CODE_REVIEW.md` for the detailed review.
