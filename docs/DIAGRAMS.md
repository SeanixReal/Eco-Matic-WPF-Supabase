# Eco-Matic Diagrams

This document contains presentation-ready Mermaid diagrams for the current codebase.

## 1. Entity Relationship Diagram (ERD)

```mermaid
erDiagram
    ROLES ||--o{ USERS : assigns
    VENDING_MACHINES ||--o{ USERS : optional_assignment
    VENDING_MACHINES ||--o{ MACHINE_INVENTORY : contains
    ITEMS ||--o{ MACHINE_INVENTORY : stocked_in
    VENDING_MACHINES ||--o{ SALES_TRANSACTIONS : records
    ITEMS ||--o{ SALES_TRANSACTIONS : sold_as
    VENDING_MACHINES o|--o{ EVENT_LOGS : logs_for
    VENDING_MACHINES ||--o{ RECEIPT_SESSIONS : issues
    RECEIPT_SESSIONS ||--o{ RECEIPT_SESSION_LINES : contains
    VENDING_MACHINES ||--o{ ESP32_TELEMETRY : reports
    VENDING_MACHINES ||--o{ ESP32_COMMANDS : receives

    ROLES {
        int role_id PK
        string role_name
    }

    USERS {
        int user_id PK
        string username
        string password_hash
        int role_id FK
        int assigned_machine_id FK
        datetime created_at
    }

    VENDING_MACHINES {
        int machine_id PK
        string location_name
        string address_text
        float latitude
        float longitude
        string status
        datetime created_at
    }

    ITEMS {
        int item_id PK
        string name
        string type
        decimal price
        int calories
        string image_path
        string dispense_message
        string examine_message
    }

    MACHINE_INVENTORY {
        int inventory_id PK
        int machine_id FK
        int item_id FK
        string slot_id
        int stock_level
        int max_capacity
        decimal slot_price
    }

    SALES_TRANSACTIONS {
        int transaction_id PK
        int machine_id FK
        int item_id FK
        decimal amount_paid
        datetime transaction_date
        uuid client_sync_id
    }

    EVENT_LOGS {
        int log_id PK
        string event_type
        string description
        int machine_id FK
        datetime log_date
        uuid client_sync_id
    }

    CUSTOMERS {
        string rfid_tag PK
        string email
        string password_hash
        int eco_credits
        datetime registered_date
    }

    RECEIPT_SESSIONS {
        bigint receipt_session_id PK
        uuid client_sync_id
        string receipt_number
        int machine_id FK
        datetime session_started_at
        datetime session_ended_at
        decimal total_amount
        decimal amount_paid
        decimal change_amount
        int recycle_points_total
        string source
        datetime created_at
    }

    RECEIPT_SESSION_LINES {
        bigint receipt_session_line_id PK
        bigint receipt_session_id FK
        int line_order
        string entry_type
        string slot_id
        string item_name
        int quantity
        decimal unit_price
        decimal line_total
        string recycle_material
        int recycle_pieces
        int recycle_points
    }

    ESP32_TELEMETRY {
        int telemetry_id PK
        int machine_id FK
        string device_id
        decimal temperature
        decimal humidity
        bool door_open
        string power_status
        datetime recorded_at
    }

    ESP32_COMMANDS {
        int command_id PK
        int machine_id FK
        string command_type
        json payload
        string status
        datetime created_at
        datetime executed_at
    }
```

## How to Explain the ERD

- `roles` and `users` implement role-based access control.
- `vending_machines` allows the system to scale beyond one physical machine and now stores machine name plus optional physical location data.
- `items` is the master catalog of products.
- `machine_inventory` is the junction table that connects a machine to an item and stores slot, stock, capacity, and optional machine-specific price.
- `sales_transactions` stores every completed sale.
- `event_logs` stores audit-style system activity such as purchases and recycling actions.
- `customers` stores RFID users and eco-credit balances.
- `receipt_sessions` and `receipt_session_lines` store receipt-level history for completed vending sessions.
- `esp32_telemetry` and `esp32_commands` extend the schema for machine-side telemetry and command delivery.

Important clarification:

`customers` is currently not connected by foreign key to `sales_transactions`. In this implementation, the customer RFID workflow is for registration and saving recycle points, while sales are recorded separately.

## 2. Class Diagram

```mermaid
classDiagram
    class MainWindow {
        -ArduinoService _arduino
        -SupabaseStore _db
        +BtnCustomer_Click()
        +BtnAdmin_Click()
        +Arduino_OnCardScanned()
    }

    class AdminWindow {
        -string _currentUserRole
        -int? _assignedMachineId
        +SetActiveView(string)
        +LoadDashboardMetrics()
        +LoadInventoryGrid(int)
        +LoadCatalogItems()
        +LoadSalesData()
    }

    class CustomerWindow {
        -decimal _insertedMoney
        -ArduinoService _arduino
        +RefreshProducts()
        +SelectButton_Click()
        +StartDispenseFeedback(VendingItem)
    }

    class DataStore {
        <<static>>
        +Products : List~Product~
        +Transactions : List~Transaction~
        +ActiveMachineId : int
        +PendingPoints : int
        +Initialize(int)
        +SaveInventory()
        +LogEvent(string, string, decimal)
        +RecordSale(int, decimal)
    }

    class OfflineSyncCoordinator {
        <<singleton>>
        +InitializeApplication()
        +TrySyncIfOnline()
        +GetMachineLookupForCustomer()
        +GetMachineInventory(int)
        +SaveInventorySnapshot(...)
        +QueueEventLog(...)
        +QueueSale(...)
        +QueueReceiptSession(...)
    }

    class SupabaseStore {
        +AuthenticateUser(string, string)
        +GetVendingMachines()
        +GetMachineInventory(int)
        +GetCatalogItems()
        +AddCatalogItem(...)
        +UpdateCatalogItem(...)
        +AddItemToMachineSlot(...)
        +UpdateMachineInventoryAssignment(...)
        +RecordSale(int, int, decimal)
        +GetFilteredSales(DateTime, string)
        +GetCustomers()
        +UpdateCustomerCredits(string, int)
    }

    class OfflineMySqlStore {
        +EnsureCreated()
        +GetCachedVendingMachinesLookup()
        +GetCachedMachineInventory(int)
        +SaveInventorySnapshot(...)
        +GetPendingQueue()
        +ReplaceCache(...)
    }

    class MapLocationService {
        +ReverseGeocodeAsync(double, double)
    }

    class MapPickerWindow {
        +SelectedAddress : string
        +SelectedLatitude : double?
        +SelectedLongitude : double?
    }

    class SupabaseClient {
        +Instance
        +GetAsync(string, string)
        +PostAsync(string, object)
        +PatchAsync(string, string, object)
        +DeleteAsync(string, string)
        +RpcAsync(string, object)
    }

    class ArduinoService {
        +OnCardScanned
        +Start()
        +Stop()
        +SendResponse(bool)
        +SendStateCommand(string)
        +SendMessage(string)
    }

    class ImageLoader {
        <<static>>
        +LoadProductImage(string)
    }

    class VendingItem {
        <<abstract>>
        +Id : int
        +DbInventoryId : int
        +Name : string
        +Price : decimal
        +Stock : int
        +ImagePath : string
        +DispenseMessage : string
        +ExamineMessage : string
        +Examine()
    }

    class Product {
        +Type : ProductType
        +Create(...)
    }

    class SnackItem {
        +Calories : int
    }

    class DrinkItem {
        +Calories : int
        +VolumeMl : int
    }

    class MiscItem

    class IHasCalories {
        <<interface>>
    }

    class IHasVolume {
        <<interface>>
    }

    class Transaction {
        +Id : int
        +Date : DateTime
        +TotalAmount : decimal
        +AmountPaid : decimal
        +Change : decimal
    }

    class TransactionItem {
        +ProductId : int
        +ProductName : string
        +Quantity : int
        +UnitPrice : decimal
        +LineTotal : decimal
    }

    class RecycleEntry {
        +Material : RecycleMaterial
        +Pieces : int
        +PointsPerPiece : int
        +TotalPoints : int
    }

    class LoginWindow
    class MachineSelectionWindow
    class CustomerRegistrationWindow
    class CustomerDashboardWindow
    class InventoryItemWindow
    class CatalogItemWindow
    class MapPickerWindow
    class ReceiptWindow

    MainWindow --> ArduinoService : listens_for_RFID
    MainWindow --> SupabaseStore : authenticates_and_checks_customers
    MainWindow ..> LoginWindow
    MainWindow ..> MachineSelectionWindow
    MainWindow ..> CustomerRegistrationWindow
    MainWindow ..> CustomerDashboardWindow
    MainWindow ..> CustomerWindow

    AdminWindow --> SupabaseStore : CRUD_and_reports
    AdminWindow ..> InventoryItemWindow
    AdminWindow ..> CatalogItemWindow
    AdminWindow ..> MapPickerWindow

    CustomerWindow --> DataStore : uses_session_state
    CustomerWindow --> ArduinoService : updates_LCD
    CustomerWindow ..> ImageLoader
    CustomerWindow ..> ReceiptWindow

    CustomerRegistrationWindow --> SupabaseStore : register_customer
    CustomerDashboardWindow --> SupabaseStore : load_and_save_credits
    MapPickerWindow --> MapLocationService : reverse_geocodes
    DataStore --> OfflineSyncCoordinator : caches_and_replays
    OfflineSyncCoordinator --> OfflineMySqlStore : local_cache
    OfflineSyncCoordinator --> SupabaseStore : cloud_sync
    SupabaseStore --> SupabaseClient : REST_calls

    VendingItem <|-- Product
    Product <|-- SnackItem
    Product <|-- DrinkItem
    Product <|-- MiscItem
    SnackItem ..|> IHasCalories
    DrinkItem ..|> IHasCalories
    DrinkItem ..|> IHasVolume

    DataStore o-- Product
    DataStore o-- Transaction
    Transaction *-- TransactionItem
    Transaction *-- RecycleEntry
```

## How to Explain the Class Diagram

- `MainWindow` is the entry controller of the application.
- `AdminWindow` and `CustomerWindow` are the two major use-case windows.
- `DataStore` manages temporary in-memory session state for customer mode.
- `OfflineSyncCoordinator` bridges customer-mode local caching with Supabase replay.
- `SupabaseStore` is the application service that hides backend details from the UI.
- `SupabaseClient` is the low-level HTTP client.
- `ArduinoService` handles event-driven hardware communication.
- `VendingItem` and its subclasses show inheritance and polymorphism.
- `Transaction`, `TransactionItem`, and `RecycleEntry` model what happens during a purchase session.

## 3. Difference Between ERD and Class Diagram

Use this sentence during your defense:

> The ERD explains how persistent data is stored in the database, while the class diagram explains how the software objects collaborate at runtime inside the WPF application.

That distinction is important because not every class becomes a table, and not every table becomes a rich domain object.

## 4. Buying Process Flow

```mermaid
graph TD
    A[Start customer session] --> B[Select vending machine]
    B --> C[DataStore initializes active machine inventory]
    C --> D[Customer inserts money or earns recycle points]
    D --> E{Enough balance for item?}
    E -- No --> D
    E -- Yes --> F[Customer selects product]
    F --> G{Item in stock?}
    G -- No --> F
    G -- Yes --> H[Decrease in-memory stock]
    H --> I[Save inventory to local cache or Supabase]
    I --> J[Queue or write sale and event records]
    J --> K[Show dispense feedback and receipt]
    K --> L[Return remaining change]
    L --> M[End session]
```

## How to Explain the Buying Process

- `DataStore.Initialize()` loads the selected machine inventory before vending begins.
- `CustomerWindow` manages balance checking and stock validation in the UI.
- `DataStore.SaveInventory()` persists stock changes through the sync layer.
- `DataStore.LogEvent()` and `DataStore.RecordSale()` either write immediately or queue replay work, depending on connectivity.
- `ReceiptWindow` finishes the customer-facing flow.
