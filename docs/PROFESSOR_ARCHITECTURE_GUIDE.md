# Professor Architecture Guide

This document is written for presentation and defense. You can use it as your speaking guide when explaining the architecture of Eco-Matic.

For a deeper class-by-class explanation and database Q&A script, use `docs/PROFESSOR_CLASS_DATABASE_QA.md` together with the separated diagrams linked from `docs/DIAGRAMS.md`.

## 1. Short Opening Script

You can start with this:

> Eco-Matic is a WPF desktop application for a smart vending machine with recycling incentives. Architecturally, it combines a presentation layer, a service layer, a relational backend accessed through Supabase REST, and an Arduino hardware integration layer for RFID scanning and LCD feedback.

That opening already tells your professor that the project is not only a UI, but a complete integrated system.

## 2. Best Order for Explaining the Project

Use this order during your discussion:

1. explain the high-level architecture
2. explain the ERD
3. explain the class diagram
4. explain one end-to-end runtime flow
5. end with your design decisions

This order works well because it moves from broad structure to implementation detail.

## 3. How to Explain the High-Level Architecture

Say that the project is divided into four parts:

### A. Presentation Layer

- built with WPF
- contains `MainWindow`, `CustomerWindow`, `AdminWindow`, and the dialog windows
- responsible for user interaction only

### B. Session and Application Layer

- centered around `DataStore`
- temporarily stores the active machine, loaded products, current transaction, and pending recycle points

### C. Service Layer

- centered around `SupabaseStore`
- hides all backend operations behind simple methods
- also includes `SupabaseSessionCoordinator` for customer-mode Supabase availability and session persistence
- also includes `ArduinoService` for hardware communication and `ImageLoader` for safe image loading

### D. Data Layer

- live relational tables such as `users`, `items`, `machine_inventory`, `sales_transactions`, and `receipt_sessions`
- accessed through `SupabaseClient` using HTTP requests to Supabase
- customer mode now uses Supabase directly; the old local database fallback path has been removed

## 4. How to Explain the ERD

When you show the ERD, focus on the meaning of each table and the reason the relationship exists.

### Core explanation

- `roles` defines the allowed system roles.
- `users` stores admin and inventory manager accounts.
- `vending_machines` stores each physical machine, including its machine name, editable address, and optional map coordinates.
- `items` is the master catalog of products.
- `machine_inventory` is the most important bridge table because it tells us which machine contains which item in which slot, at what stock level, and at what machine-specific item price if needed.
- `sales_transactions` stores every purchase.
- `event_logs` stores activity history for auditing, dashboard reporting, restock/slot changes, and RFID account history.
- `customers` stores RFID users and their eco-credit balances.

### Strong point to say

> I separated `items` from `machine_inventory` because the same product definition can be reused by different machines, while each machine still has its own slot and stock values.

That is a good database normalization argument.

For catalog deletion, the app clears `machine_inventory` assignments first so the vending machine becomes empty in those slots. Then it soft-deletes the `items` row by setting `is_active = false`, `deleted_at`, and `deleted_reason`. Active catalog and inventory screens filter to `is_active = true`, while historical sales reports can still join old sales to the original item name and type.

You can extend it with:

> I also allow an optional machine-specific item price, so one global item can still be sold at different prices depending on the machine location. If the same item appears in several slots in the same machine, the app keeps that item price consistent across those slots.

You can also say:

> I extended the machine table so the admin can save both a readable machine name and a physical address. The address can be typed manually or selected from a map and reverse-geocoded into text.

### Important clarification

If your professor asks why `customers` is not connected to sales by foreign key, say:

> In the current implementation, RFID customers are mainly used for registration and saving recycle credits. Sales are recorded independently, and the connection between vending activity and customer RFID is handled at application level instead of a direct foreign key relationship.

The customer dashboard history follows that same rule: it reads RFID-tagged purchase entries from `event_logs` and displays item, quantity, and paid cash/points rather than joining customers directly to sales.

That answer is honest and technically correct.

## 5. How to Explain the Class Diagram

When you switch from ERD to class diagram, say this:

> The ERD shows stored data, but the class diagram shows how the software behaves at runtime.

Then explain the classes in groups instead of one by one.

### A. Entry and Flow Control

- `MainWindow` is the starting point of the application
- it routes the user to customer mode or admin mode
- it also listens for RFID scans through `ArduinoService`

### B. Main Use-Case Windows

- `CustomerWindow` handles vending logic
- `AdminWindow` handles management features
- `CatalogItemWindow` handles shared item catalog editing
- `InventoryItemWindow` handles machine-slot assignment and stock editing
- `MapPickerWindow` helps the admin choose a machine site visually and auto-fill the address

### C. Shared Services

- `SupabaseStore` is the main data access service used by the windows
- `SupabaseClient` is the lower-level HTTP helper used by `SupabaseStore`
- `MapLocationService` reverse-geocodes the selected map coordinates into a physical address
- `ArduinoService` handles serial communication
- `ImageLoader` makes image loading more fault-tolerant

### D. Domain Model

- `VendingItem` is the abstract parent class
- `Product` is the base concrete product type
- `SnackItem`, `DrinkItem`, and `MiscItem` are specialized subclasses
- `Transaction`, `TransactionItem`, and `RecycleEntry` represent what happens during a vending session

### OOP points to mention

- inheritance: `SnackItem`, `DrinkItem`, and `MiscItem` inherit from `Product`
- abstraction: `VendingItem` is abstract and defines the common item structure
- polymorphism: each item type can override `Examine()`
- encapsulation: UI classes do not directly perform REST calls; they go through `SupabaseStore`

## 6. One End-to-End Flow You Can Explain

The easiest runtime flow to defend is the vending flow:

1. `MainWindow` opens machine selection
2. the machine selection screen shows the machine name and address so the user can identify the correct kiosk
3. `DataStore.Initialize()` loads the chosen machine inventory through `SupabaseSessionCoordinator`
4. `CustomerWindow` displays products from `DataStore.Products`
5. the user inserts money and selects an item
6. stock is reduced in memory
7. `DataStore.SaveInventory()` updates stock through Supabase
8. `DataStore.LogEvent()` writes an event log through Supabase
9. `DataStore.RecordSale()` stores the sale through Supabase
10. `ReceiptWindow` can show the selected machine name and address on-screen
11. the printed receipt can include the selected machine name and address
12. receipt item lines are grouped by product, unit price, and payment mode, so buying the same product from two slots produces one quantity line unless one purchase used eco-points

This flow shows UI, application state, backend, and business logic all working together.

You can also explain the admin inventory flow:

1. admin creates or edits a shared product in the `Items` tab
2. admin assigns that product to a specific machine slot in the `Inventory` tab
3. that slot stores machine-specific stock and optional machine item price
4. restock and slot changes write audit entries into `event_logs`
5. customer mode reads the configured slot and shows the correct item for that machine

You can also explain the sales analytics flow:

1. admin opens the Sales Report, which defaults to Week
2. `SupabaseStore.GetFilteredSales()` loads the selected date range and optional machine scope
3. `AdminWindow.UpdateSalesReportVisuals()` groups the rows by item, machine, category, and period
4. the UI shows KPI cards, trend bars, product mix, best-selling items, machine revenue, category revenue, peak periods, and transaction details

You can also explain the machine-location setup flow:

1. admin opens `AddMachineWindow` or `EditMachineWindow`
2. admin enters a machine name
3. admin can open `MapPickerWindow` and click the machine position on a map
4. `MapLocationService` reverse-geocodes that point into a readable address
5. admin can still manually edit the address before saving
6. `SupabaseStore` saves the machine name, address, and coordinates into `vending_machines`
7. inventory assignment is handled afterward in the Inventory view, so a new machine can be registered empty and stocked later

## 6.1 Honest Current Limitations

If your professor asks what still needs improvement, you can answer honestly with these points:

- the customer UI currently supports 12 visible vending slots, so backend inventory should stay aligned with that limit
- the inventory model is now intentionally split between a shared item catalog and machine-specific slot assignment
- RFID account history is application-layer event-log matching because customers are not foreign-keyed to sales records
- the project uses a practical MVVM-lite approach rather than a full MVVM architecture

That answer is strong because it shows you understand both the design and the remaining technical debt.

## 7. Questions Your Professor Might Ask

### Why did you use code-behind instead of full MVVM?

Suggested answer:

> I used an MVVM-lite approach because it kept the project manageable for the scope and deadline. I still separated reusable logic into service classes like `SupabaseStore`, `ArduinoService`, and `ImageLoader` so the UI code is not directly coupled to backend or hardware implementation details.

### Why is `DataStore` static?

Suggested answer:

> I used `DataStore` as a shared session state container for customer mode. It simplifies passing the active inventory and transaction state across the customer-facing workflow without repeatedly querying the backend.

### Why use `machine_inventory` instead of putting stock directly in `items`?

Suggested answer:

> Stock belongs to a machine slot, not to the global product definition. A product like Coca Cola can exist in many machines, but each machine can have a different stock level and slot assignment.

### Why not always physically delete sold catalog items?

Suggested answer:

> Sales reports still need the old product name and type. I use soft delete on `items`, so deleted products disappear from active catalog and vending workflows but remain available for historical joins from `sales_transactions`.

### How is role-based access control enforced?

Suggested answer:

> Authentication happens in `SupabaseStore.AuthenticateUserAccess()`, and then `AdminWindow` checks the returned role and assigned machine IDs. Based on that role, it hides restricted views and limits machine access for inventory managers.

### How does hardware interact with the desktop app?

Suggested answer:

> `ArduinoService` communicates with the Arduino over serial. When an RFID is scanned, the service raises an event. `MainWindow` listens to that event and decides whether to open customer registration or the customer dashboard. The app also sends LCD messages and validation responses back to the Arduino.

### Why is there both a database service and a `DataStore`?

Suggested answer:

> `SupabaseStore` handles cloud persistence, `SupabaseSessionCoordinator` centralizes customer-mode Supabase availability and session writes, and `DataStore` holds temporary in-memory state for the active vending session. That lets the customer UI react quickly while still keeping backend access in service classes.

### Does the app support disconnected operation?

Suggested answer:

> No. The current build is intentionally Supabase-only. If Supabase is unreachable, customer/admin data features show a connectivity message instead of using a local database fallback.

### Why add map-based machine location if there is already a text field?

Suggested answer:

> The text field keeps the feature practical because the address can always be edited manually, while the map improves speed and accuracy by letting the admin point to the actual machine site and auto-fill the nearest address.

## 8. Files to Open During Defense

If you want a strong live walkthrough, open these files:

- `docs/DIAGRAMS.md`
- `docs/PROFESSOR_CLASS_DATABASE_QA.md`
- `MainWindow.xaml.cs`
- `CustomerWindow.xaml.cs`
- `AdminWindow.xaml.cs`
- `Data/DataStore.cs`
- `Data/SupabaseStore.cs`
- `Data/SupabaseStore_Customers.cs`
- `Data/SupabaseClient.cs`
- `Data/MapLocationService.cs`
- `Data/ArduinoService.cs`
- `MapPickerWindow.xaml.cs`
- `Models/VendingItem.cs`
- `Models/Product.cs`
- `Models/Transaction.cs`

## 9. Best Final Closing Statement

You can end with:

> The main architectural strength of Eco-Matic is that it separates UI behavior, backend access, domain modeling, and hardware communication while still keeping the project simple enough to maintain as a student system.

That gives a clean and confident ending to your explanation.
