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
  - shows a startup Supabase connectivity badge so demo readiness is visible before opening data-heavy flows
- `CustomerWindow.xaml.cs`
  - handles the vending machine experience
  - manages money insertion, QR-paid balance, item selection, recycle points, point payment, and receipt flow
- `AdminWindow.xaml.cs`
  - acts as the admin control center
  - loads dashboard, inventory, global items, sales, logs, machines, users, and customer data for admins
  - limits inventory managers to the inventory page for their assigned machines only
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
    - displays sale lines, recycle lines, cash/QR paid amount, eco-points used, eco-points earned, RFID balance when available, or a clear not-saved note for guest recycle points

### B. Application and Session Layer

This layer keeps temporary application state that is useful while a vending session is active.

- `Data/DataStore.cs`
  - stores the active machine ID
  - loads inventory from the backend into memory
  - keeps the in-session product list
  - tracks the latest transaction and pending recycle points

This is important because not every UI action needs to query the backend for display state. The customer screen works against an in-memory session model, while persistence is sent through the Supabase-backed session coordinator.

Important limitation:

- `DataStore` is still the in-memory session layer used by the customer UI
- `SupabaseSessionCoordinator` is the Supabase-only path for customer-mode inventory, sales, event logs, and receipts
- customer mode, admin mode, and RFID customer account writes now require live Supabase connectivity

### C. Service and Integration Layer

This layer connects the UI to external systems.

- `Data/SupabaseStore.cs`
  - main application data service
  - contains CRUD and reporting operations
  - writes audit events for inventory, restock, catalog, machine, customer-credit, sale, and recycle activity through the existing `event_logs` table
  - returns `DataTable` objects because WPF `DataGrid` binding is used throughout the admin screens
- `Data/SupabaseStore_Customers.cs`
  - extends `SupabaseStore` with customer RFID operations
  - implemented as a partial class to keep customer-specific logic separate
- `Data/SupabaseSessionCoordinator.cs`
  - checks Supabase availability for customer mode
  - routes customer-session inventory, sale, log, and receipt persistence to `SupabaseStore`
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
  - point-payment receipt fields are kept on `Transaction` and `TransactionItem` so the customer-facing receipt can distinguish cash/QR payment from eco-credit usage
  - receipt sale lines group the same catalog item and unit price together across different slots, while cash/QR and eco-point purchases remain separate lines

## 3. Main Runtime Flows

### 3.1 Customer Vending Flow

1. `MainWindow` opens `MachineSelectionWindow`.
2. The machine selection screen shows the machine name and address to help the customer identify the correct kiosk.
3. The selected machine ID is stored through `DataStore.Initialize(machineId)`.
4. `DataStore` loads the machine inventory through `SupabaseSessionCoordinator`.
5. `CustomerWindow` displays the 12-slot vending layout using the in-memory `DataStore.Products` list.
6. When the customer buys an item:
   - money is validated in the UI
   - stock is decreased in memory
   - `DataStore.SaveInventory()` updates Supabase stock through `SupabaseSessionCoordinator`
   - `DataStore.LogEvent()` writes a customer event log through Supabase
   - `DataStore.RecordSale()` writes a sales record through Supabase
   - a receipt can be shown through `ReceiptWindow`
   - the on-screen receipt can show the selected machine name and address
   - the on-screen and printed receipt include cash/QR paid amount, point usage, recycle points earned, and remaining point balances
   - receipt item lines show quantity and product name, not vending slot labels

Important current behavior:

- products are placed into customer slots using the real normalized `slot_id`
- effective price comes from the machine item override when present, otherwise the global default item price
- if the same catalog item is assigned to multiple slots in one machine, editing the machine item price on any one of those slots updates every matching slot in that machine

### 3.2 RFID and Eco-Credit Flow

1. `ArduinoService` receives serial data from the RFID scanner.
2. `MainWindow` listens to `OnCardScanned`.
3. If the RFID exists:
   - `CustomerDashboardWindow` is opened
   - pending recycle points are saved into the `customers` table
   - the active customer window first attaches any current-session purchases without an RFID to this card, then transaction history is shown from exact matching RFID purchase rows in `event_logs`
4. If the RFID does not exist:
   - `CustomerRegistrationWindow` is opened first
   - after registration, the dashboard can open

Important implementation note:

- recycle points are persisted to the customer account; if RFID is already linked, new recycle points are saved immediately instead of waiting for another scan
- the customer window keeps a session RFID lock once purchases exist, so a later scan from another RFID can view that account but cannot steal the current session's transaction history
- purchases can use cash, QR-paid balance, or available eco-points; RFID identifies the account used for saved points and customer transaction history
- point purchases no longer inflate the PHP paid amount on the receipt; the session records points spent separately from cash/QR value
- if a customer buys the same product from multiple slots, the receipt combines those purchases by product, unit price, and payment mode

### 3.3 Admin Flow

1. `MainWindow` opens `LoginWindow`.
2. `SupabaseStore.AuthenticateUserAccess()` validates the username and password and returns the role plus assigned machine IDs.
3. `AdminWindow` loads with role-based restrictions.
4. If the role is `Admin`, the first loaded view is `Dashboard`.
5. If the role is `Inventory Manager`, the user is routed directly to Inventory and every other page is hidden.
6. The admin window uses one shell window and switches views with `Visibility` toggling instead of opening many separate pages.
7. Machine setup now stores:
   - a machine name in `vending_machines.location_name`
   - an editable address in `vending_machines.address_text`
   - optional map-picked coordinates in `vending_machines.latitude` and `vending_machines.longitude`

Important admin split:

- the `Items` tab manages the shared global catalog in `items`
- the `Inventory` tab manages per-machine slot assignment, stock, and optional machine item price override in `machine_inventory`
- deleting a catalog item first clears matching `machine_inventory` slot assignments so the customer vending machine refreshes with those slots empty
- catalog delete is a soft delete on `items`: `is_active = false`, `deleted_at`, and `deleted_reason`; active catalog and inventory assignment screens filter on `is_active = true`
- the `Inventory` tab has a selected-row quick action that restocks only the chosen item to its max capacity
- the `Machines` tab manages machine identity and physical placement information; a new machine can be registered without immediately assigning stock
- the `Sales Report` tab can be filtered by date period and by a single vending machine, or left on all machines
- the `Sales Report` tab defaults to the Week filter and calculates revenue trend, product mix, best-selling items, machine revenue, category revenue, peak sales periods, top machine, transaction count, unit count, and average sale from the filtered sales rows
- the `Logs` tab defaults to Week and includes the machine name when an event is machine-scoped
- stock monitoring now includes the vending machine name beside low-stock rows so alerts show where the problem is

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
- `machine_inventory` stores which machine has which item in which slot, with stock, capacity, and optional machine-specific item price

That separation avoids duplicated product definitions across machines.

Catalog deletion respects that split: slot rows are removed immediately, while the catalog row is soft-deleted so `sales_transactions` can still join back to the original item name/type for historical reporting.

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
- `recyclable_items`
- `qr_payment_intents`
- `user_machine_assignments`

One important detail for presentation:

- `customers` is logically related to RFID and eco-credits
- but it is not currently linked by foreign key to `sales_transactions`
- customer transaction history is therefore derived from purchase-related `event_logs` entries that include the RFID in the description, not from a direct customer-sales relationship

Important vending-machine detail:

- `vending_machines.location_name` now acts as the machine name shown in admin and customer selection
- `vending_machines.address_text` stores the human-readable address
- `vending_machines.latitude` and `vending_machines.longitude` store optional map-selected coordinates

That means your ERD should show `customers` as an independent table in the current implementation.

Current staff-role detail:

- live staff roles are `Admin` and `Inventory Manager`
- `Operator` and `Viewer` are no longer exposed in the app
- every inventory manager must be assigned to at least one vending machine
- multiple inventory-manager machine assignments live in `user_machine_assignments`
- `users.assigned_machine_id` remains as a compatibility primary assignment
- demo sales data exists in `sales_transactions` across roughly one year so Day, Week, Month, Year, All Time, and machine-scoped sales reports can be demonstrated

## 7. What to Emphasize During Defense

- The project is not just a UI. It integrates desktop software, a relational backend, and external hardware.
- The design separates user interface logic from database and hardware services.
- The admin side and customer side share the same backend but solve different use cases.
- The architecture uses inheritance in the product model and event-driven programming in the Arduino integration.
- The schema supports multiple vending machines, not just one machine.

## 8. Known Documentation Mismatch in the Repository

Some older archived or non-canonical files in the repository may still describe a previous local database direction.

For the current codebase, the accurate implementation is:

- `SupabaseStore`
- `SupabaseClient`
- `SupabaseSessionCoordinator`
- REST access to Supabase

Conceptually, the schema is still relational, so the ERD explanation remains valid, but the access technology has changed and the live schema now also includes receipt history, QR payment intents, recyclable item definitions, and multi-machine staff assignments.

For the live Supabase audit status, migrations, and current auth/RLS findings, see `docs/SUPABASE_AUDIT.md` and `docs/SUPABASE_MCP_ANALYSIS.md`.

## 9. Current Review Notes

The latest review found these implementation caveats:

- the customer UI has 12 visible slots, and the backend now enforces that limit for machine inventory
- `DataStore.Initialize()` now maps products to customer slots using the real normalized `slot_id`
- RFID is used for registration and recycle-credit saving, not for direct purchase payment
- password fields are currently stored and compared directly even though some field names still say `password_hash`
- the image strategy is local-first rather than Supabase Storage-first to keep classroom/demo behavior reliable
- customer mode, admin mode, and RFID persistence require live Supabase connectivity

See `docs/CODE_REVIEW.md` for the detailed review.
