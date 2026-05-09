# Professor Class and Database Q&A Guide

This document explains the Eco-Matic classes, database connection design, and common technical questions.

Use it together with `docs/DIAGRAMS.md`. The diagrams are separated under `docs/diagrams/`, while this guide provides the supporting explanations.

## 1. One-Minute Overview

Eco-Matic is a .NET WPF smart vending machine application. It has two main modes:

- customer mode for choosing a machine, buying products, recycling items, QR payment, receipts, and RFID credit saving
- admin mode for managing machines, users, product catalog items, machine slot inventory, sales reports, logs, and recyclable item definitions

The code uses an MVVM-lite structure. Window behavior stays in `.xaml.cs` files, while reusable backend, hardware, image, payment, receipt, and sync logic is separated into service/helper classes.

The live cloud database is Supabase PostgreSQL. The WPF app does not connect to PostgreSQL with a raw database driver. Instead, it uses Supabase PostgREST through `SupabaseClient`, and higher-level application operations go through `SupabaseStore`.

## 2. The Most Important Classes

### `MainWindow`

`MainWindow` is the entry window. It routes users into customer mode or admin mode and listens for RFID scans from `ArduinoService`.

Explanation:

> `MainWindow` is the traffic controller. It decides whether the user goes to customer flow, admin login, RFID registration, or the customer dashboard.

Important connections:

- uses `ArduinoService` for RFID scan events and hardware responses
- uses `SupabaseStore` to check whether an RFID customer exists
- opens `MachineSelectionWindow`, `CustomerWindow`, `LoginWindow`, `AdminWindow`, `CustomerRegistrationWindow`, and `CustomerDashboardWindow`

### `CustomerWindow`

`CustomerWindow` is the vending screen. It displays the 12 visible vending slots, handles money, QR payment, recycle points, selected products, stock reduction, dispense feedback, and receipts.

Explanation:

> `CustomerWindow` is where the business flow happens from the customer's perspective. It does not own the database directly; it uses `DataStore` for the active session and services for payment, hardware messages, and receipt output.

Important connections:

- reads products from `DataStore.Products`
- updates stock through `DataStore.SaveInventory`
- records purchases through `DataStore.RecordSale`
- saves full receipt sessions through `DataStore.SaveCompletedReceipt`
- uses `QrPaymentService` for QR payment intents and status checks
- uses `ArduinoService` for LCD/status messaging

### `AdminWindow`

`AdminWindow` is the management console. Admin users can access dashboard, catalog, inventory, machine, user, recyclable item, sales, stock monitoring, customer, and event log workflows. Inventory managers are restricted to inventory management for their assigned vending machines.

Explanation:

> `AdminWindow` is the main back-office console. It calls `SupabaseStore` methods instead of writing SQL directly in the UI.

Important connections:

- uses `SupabaseStore` for CRUD and reports
- opens `CatalogItemWindow` for global item editing
- opens `InventoryItemWindow` for machine slot assignment
- opens `AddMachineWindow`, `EditMachineWindow`, and `MapPickerWindow` for machine management
- opens `UserEditorWindow`, `RecyclableItemWindow`, `RestockWindow`, and `EventLogWindow`

### `DataStore`

`DataStore` is a static in-memory session state holder for customer mode.

Explanation:

> `DataStore` stores the current selected machine, active product list, pending recycle points, and current transaction history for the vending session. It keeps the customer UI responsive and prevents every button click from directly querying Supabase.

Important responsibilities:

- tracks `ActiveMachineId`, `ActiveMachineDisplayName`, and `ActiveMachineAddress`
- stores active products in `Products`
- stores active recyclable definitions in `RecyclableItems`
- stores completed session transactions in `Transactions`
- calls `SupabaseSessionCoordinator` for inventory saves, sale recording, event logging, and receipt persistence

### `SupabaseStore`

`SupabaseStore` is the main application database service.

Explanation:

> `SupabaseStore` is a service layer. It translates app-level operations like "get inventory", "record sale", or "register RFID customer" into Supabase REST calls.

Important responsibilities:

- authentication through `AuthenticateUser`
- machine CRUD through `GetVendingMachines`, `CreateMachine`, `UpdateMachine`, and `DeleteMachine`
- global product catalog CRUD through `GetCatalogItems`, `AddCatalogItem`, `UpdateCatalogItem`, and `DeleteCatalogItem`
- catalog deletion clears machine slot assignments first, then soft-deletes the `items` row so sales reports keep historical item labels
- machine inventory CRUD through `GetMachineInventory`, `AddItemToMachineSlot`, `UpdateMachineInventoryAssignment`, `RestockInventoryItem`, and `UpdateStock`
- sales/report data through `RecordSale`, `GetFilteredSales`, `GetSalesTotals`, and `GetDashboardMetrics`
- machine-scoped sales reporting through the optional machine filter in `GetFilteredSales`
- RFID customer operations through the partial class in `SupabaseStore_Customers.cs`
- receipt history persistence through `InsertQueuedReceiptSession`

### `SupabaseClient`

`SupabaseClient` is the low-level HTTP wrapper for Supabase.

Explanation:

> `SupabaseClient` is the only class that knows the Supabase URL, API key, PostgREST base URL, and HTTP methods. This keeps networking details out of the UI.

Important responsibilities:

- reads `ECOMATIC_SUPABASE_URL` and the Supabase API key from environment configuration
- builds the `/rest/v1` base URL
- sends `GET`, `POST`, `PATCH`, and `DELETE` requests
- sends RPC requests when needed
- builds Edge Function URLs through `GetFunctionUrl`

### `SupabaseSessionCoordinator`

`SupabaseSessionCoordinator` centralizes Supabase availability checks and customer-session persistence.

Explanation:

> The coordinator keeps customer-mode data access Supabase-only. It checks whether Supabase is reachable, loads machine lookup and inventory, and routes inventory, sale, log, and receipt writes to `SupabaseStore`.

Important note:

Customer mode, admin mode, and RFID account persistence all require live Supabase connectivity in the current build.

### `ArduinoService`

`ArduinoService` isolates serial hardware communication.

Explanation:

> The app does not mix serial-port code throughout the windows. `ArduinoService` starts and stops the serial listener, raises events when a card is scanned, and sends responses or LCD messages back to the Arduino.

Important responsibilities:

- listens for RFID card scan messages
- sends `VALID`, `INVALID`, state commands, and `MSG:` messages
- supports customer session active/AFK messages

### `QrPaymentService`

`QrPaymentService` handles QR payment integration through the Supabase Edge Function.

Explanation:

> QR payment is modeled as an intent. The WPF app asks the Edge Function to create a payment reference and token, displays the QR URL, and then checks whether the intent status changed to paid.

Important responsibilities:

- creates a QR intent using `qr-payment-confirm`
- checks payment status
- marks an intent paid in the demo/payment-confirmation flow

### `ReceiptPrinterService` and `EscPosReceiptFormatter`

These classes handle physical receipt printing.

Explanation:

> Receipt formatting is separated from receipt printing. `EscPosReceiptFormatter` builds the ESC/POS-style receipt content, while `ReceiptPrinterService` decides how to send it to the configured printer.

### Product Model Classes

`VendingItem` is the abstract base class for vending products. `Product` is the concrete base product class, and `SnackItem`, `DrinkItem`, and `MiscItem` specialize it.

Explanation:

> This is where the project demonstrates OOP. `VendingItem` defines the shared structure, `Product` adds product type behavior, and specialized product classes implement category-specific details like calories and volume.

OOP terms to mention:

- abstraction: `VendingItem`
- inheritance: `SnackItem`, `DrinkItem`, and `MiscItem` inherit from `Product`
- interfaces: `IHasCalories` and `IHasVolume`
- polymorphism: item types can provide different `Examine()` behavior

### Transaction Model Classes

`Transaction`, `TransactionItem`, and `RecycleEntry` represent a completed vending session.

Explanation:

> A transaction is not just one row of sales. For receipts, the app needs the full session: purchased item lines, recycled item lines, totals, amount paid, change, machine name, and receipt number.

Receipt sale lines are grouped by product, unit price, and payment mode rather than by slot. This keeps receipts customer-friendly when the same product is stocked in multiple slots, while still keeping point-paid purchases separate from cash/QR-paid purchases.

Important mapping:

- `Transaction` maps conceptually to `receipt_sessions`
- `TransactionItem` maps to sale-type rows in `receipt_session_lines`
- `RecycleEntry` maps to recycle-type rows in `receipt_session_lines`

## 3. Supporting Window Classes

These classes are smaller dialogs or support screens:

- `MachineSelectionWindow`: lets the customer choose the active vending machine.
- `LoginWindow`: collects admin username/password.
- `CustomerRegistrationWindow`: registers a new RFID customer.
- `CustomerDashboardWindow`: shows RFID customer information and saves pending recycle points. After an RFID is linked to the active vending session, later recycle points are saved to that RFID automatically.
- `CatalogItemWindow`: creates or edits global product catalog rows in `items`.
- `InventoryItemWindow`: assigns catalog items to machine slots in `machine_inventory`.
- `RecyclableItemWindow`: creates or edits recyclable item definitions.
- `AddMachineWindow` and `EditMachineWindow`: create or update machine records.
- `MapPickerWindow`: lets the admin select machine coordinates on a map and reverse-geocode the address.
- `RestockWindow`: collects restock quantity.
- `UserEditorWindow`: creates admin/inventory-manager users.
- `ReceiptWindow`: displays the final customer receipt and can trigger printing.
- `QrPaymentWindow`: displays QR payment state.
- `PointAmountWindow`: collects recycle point amount.
- `ItemDetailsWindow`, `EventLogWindow`, `AboutWindow`, and `ReadmeWindow`: display focused information.

## 4. Database Connection Explanation

The app connects to Supabase in layers:

1. WPF windows call `SupabaseStore` or `DataStore`.
2. `DataStore` calls `SupabaseSessionCoordinator` for customer-mode persistence.
3. `SupabaseSessionCoordinator` calls `SupabaseStore`.
4. `SupabaseStore` calls `SupabaseClient`.
5. `SupabaseClient` sends HTTP requests to Supabase PostgREST.
6. Supabase PostgREST reads/writes PostgreSQL tables in the `public` schema.

Short answer:

> The app does not manually open a PostgreSQL socket. Supabase exposes the database as a secure REST API, and `SupabaseClient` is the app's wrapper around that API.

Environment values:

- `ECOMATIC_SUPABASE_URL`: Supabase project URL
- Supabase API key: loaded through `AppEnvironment.GetRequiredSupabaseApiKey()`

HTTP mapping:

- `GetAsync` maps to HTTP `GET`
- `PostAsync` maps to HTTP `POST`
- `PatchAsync` maps to HTTP `PATCH`
- `DeleteAsync` maps to HTTP `DELETE`
- `RpcAsync` maps to Supabase Postgres RPC
- `GetFunctionUrl` builds URLs for Supabase Edge Functions

## 5. Database Table Explanation

### Core tables

- `roles`: defines user roles.
- `users`: stores admin/inventory-manager accounts, role assignment, and a legacy primary assigned machine.
- `user_machine_assignments`: stores one or more vending machines assigned to each inventory manager.
- `vending_machines`: stores machine name, status, address, and optional coordinates.
- `items`: stores the global catalog of products.
- `machine_inventory`: stores machine-specific slot assignments, stock, capacity, and optional machine item price.

### Transaction and reporting tables

- `sales_transactions`: simplified sale records for reports and dashboard totals.
- `event_logs`: audit-style system events.
- `receipt_sessions`: one completed customer receipt session.
- `receipt_session_lines`: line items for sales and recycling inside a receipt.

### Customer and recycling tables

- `customers`: RFID tag, email, direct password value, eco credits, and registration date.
- `recyclable_items`: configurable recycle material definitions and points per unit.

### Hardware/payment extension tables

- `qr_payment_intents`: QR payment reference, token, amount, status, and timestamps.
- `esp32_telemetry`: future/current telemetry readings from ESP32 devices.
- `esp32_commands`: command queue for ESP32 devices.

## 6. Why the Database Is Designed This Way

### Why separate `items` and `machine_inventory`?

Answer:

> `items` is the global product catalog, while `machine_inventory` is the stock inside a specific machine slot. If stock were stored in `items`, the system could not correctly support the same product in multiple machines with different quantities or prices.

### What happens when a catalog item is deleted?

Answer:

> The app first deletes any `machine_inventory` rows for that item, so the affected vending slots become empty. Then it soft-deletes the `items` row with `is_active = false`, `deleted_at`, and `deleted_reason`. Active catalog screens hide it, but sales reports can still join old transactions to the product name/type.

### Why is slot ID in `machine_inventory`?

Answer:

> Slot ID is physical placement, not product identity. The same item can be assigned to different slots in different machines, so the slot belongs to the machine inventory row.

### Why are receipts separate from sales?

Answer:

> `sales_transactions` is useful for dashboard reporting, but receipts need richer information. A receipt can include multiple product lines and recycle lines, so it uses `receipt_sessions` and `receipt_session_lines`.

### Why is `customers` not connected to `sales_transactions`?

Answer:

> In the current implementation, RFID is used for registration and saving recycle credits. Purchases are currently cash or QR based, so sales are recorded independently from customer RFID identity.

### Why use Supabase instead of direct PostgreSQL?

Answer:

> Supabase gives the app a hosted PostgreSQL database, REST API, Edge Functions, and security controls. The desktop app can use HTTP requests instead of shipping raw database connection details throughout the code.

## 7. Likely Technical Questions and Suggested Answers

### Q: Where is the database connection code?

Answer:

> The low-level connection code is in `Data/SupabaseClient.cs`. It creates an `HttpClient`, loads the Supabase URL and key from environment configuration, sets the `apikey` and `Authorization` headers, and sends requests to `/rest/v1`.

### Q: Where are SQL queries written?

Answer:

> The app mostly uses Supabase PostgREST query strings instead of handwritten SQL inside the WPF code. For example, `SupabaseStore.GetMachineInventory` builds a table request with `select`, filters, joins, and order parameters. Database structure changes are kept in SQL migration files under `docs/sql/migrations/supabase`.

### Q: What prevents the UI from knowing too much about the database?

Answer:

> UI windows call service methods like `GetCatalogItems` or `UpdateCustomerCredits`. The table names, JSON parsing, and REST details stay inside `SupabaseStore` and `SupabaseClient`.

### Q: Why use a static `DataStore`?

Answer:

> It acts as shared session state for the active customer vending flow. Since customer mode moves through product selection, payment, recycling, and receipt screens, a static session holder keeps that state available without repeatedly querying the backend.

### Q: Is this full MVVM?

Answer:

> It is MVVM-lite. The project uses WPF windows with code-behind for UI behavior, but reusable logic is still separated into services such as `SupabaseStore`, `ArduinoService`, `QrPaymentService`, and `ReceiptPrinterService`.

### Q: How does login work?

Answer:

> `LoginWindow` collects credentials, and `SupabaseStore.AuthenticateUserAccess` queries `users` joined with `roles` plus machine assignments. Admin users can access the management console. Inventory managers are routed to Inventory only and can only see vending machines assigned by the admin.

Important note:

> Passwords are stored and compared directly in the current implementation even though the column is named `password_hash`. A production system should hash and salt passwords or use Supabase Auth.

### Q: How does RFID work?

Answer:

> `ArduinoService` reads RFID data from the serial port and raises an event. `MainWindow` receives the event, checks the RFID tag through `SupabaseStore.CustomerExists`, then either opens registration or the customer dashboard. The app sends a validation response or message back to the Arduino.

### Q: How does QR payment work?

Answer:

> `QrPaymentService` calls the Supabase Edge Function `qr-payment-confirm`. The function creates a row in `qr_payment_intents` with a reference, token, amount, and pending status. The customer scans the generated URL, and the app checks whether the payment status became paid.

### Q: What is the active Supabase Edge Function?

Answer:

> The active Edge Function is `qr-payment-confirm`. It has JWT verification disabled because it is designed as a QR confirmation endpoint, and it uses its own reference/token pair to identify a payment intent.

### Q: What happens if Supabase is offline?

Answer:

> Customer mode requires live Supabase connectivity. If Supabase is unreachable, the app shows a connectivity message instead of using a local database fallback.

### Q: What are the most important database relationships?

Answer:

> The most important relationship is `vending_machines -> machine_inventory -> items`. That chain answers: which machine, which slot, which product, and how much stock. The next important relationship is `receipt_sessions -> receipt_session_lines`, which preserves a complete receipt.

### Q: Why are there both `sales_transactions` and `receipt_sessions`?

Answer:

> `sales_transactions` is a simple reporting table. `receipt_sessions` is a detailed receipt history table. The app uses both because dashboards need fast totals, while receipts need full session detail.

### Q: Where is the 12-slot limit enforced?

Answer:

> The UI has 12 visible slots, `DataStore.MaxItemSlots` is 12, `SlotIdHelper` normalizes and validates slot IDs, and `SupabaseStore.TryValidateSlotForMachine` prevents assigning more than 12 slots to one machine.

### Q: Why use local image paths instead of Supabase Storage?

Answer:

> Images are local-first so the vending UI can still load product images without depending on cloud file storage. The database stores image paths, and `ImageLoader` handles safe loading and fallback behavior.

### Q: What class should I show first during a code walkthrough?

Answer:

> Start with `MainWindow.xaml.cs`, then `CustomerWindow.xaml.cs` or `AdminWindow.xaml.cs`, then `DataStore.cs`, `SupabaseStore.cs`, and finally `SupabaseClient.cs`. That order moves from UI flow to database implementation.

## 8. Best Code Walkthrough Order

1. `MainWindow.xaml.cs`: entry routing and RFID handling
2. `CustomerWindow.xaml.cs`: customer vending workflow
3. `AdminWindow.xaml.cs`: admin workflows and role-sensitive management
4. `Data/DataStore.cs`: active customer session state
5. `Data/SupabaseSessionCoordinator.cs`: Supabase-only customer session data path
6. `Data/SupabaseStore.cs`: application-level database methods
7. `Data/SupabaseStore_Customers.cs`: RFID customer table operations
8. `Data/SupabaseClient.cs`: low-level Supabase REST client
9. `Data/QrPaymentService.cs`: Edge Function integration
10. `Models/Product.cs` and `Models/VendingItem.cs`: OOP product model
11. `Models/Transaction.cs`: receipt/session model

## 9. Strong Closing Statement

Suggested closing:

> The strongest part of Eco-Matic's architecture is that it separates responsibilities. WPF windows handle user interaction, service classes handle database, hardware, payment, and printing, model classes represent vending concepts, and Supabase stores normalized persistent data.
