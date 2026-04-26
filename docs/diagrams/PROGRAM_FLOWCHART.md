# Entire Program Flowchart

This flowchart shows the whole application from startup through customer mode, admin mode, RFID handling, QR payment, receipts, and persistence.

```mermaid
flowchart TD
    A([Launch Eco-Matic WPF App]) --> B[App loads configuration]
    B --> C[MainWindow opens]
    C --> D[Initialize ArduinoService and OfflineSyncCoordinator]
    D --> E{Supabase reachable?}
    E -- Yes --> F[Use Supabase as live data source]
    E -- No --> G{Local MySQL demo cache configured?}
    G -- Yes --> H[Use local customer-mode cache]
    G -- No --> I[Mark data source unavailable for customer mode]

    F --> J{User action}
    H --> J
    I --> J

    J -- Customer button --> K{Can enter customer mode?}
    K -- No --> K1[Show unavailable or no-machine message]
    K1 --> J
    K -- Yes --> L[Open MachineSelectionWindow]
    L --> M{Machine selected?}
    M -- No --> J
    M -- Yes --> N[Set DataStore active machine name, address, and id]
    N --> O[DataStore.Initialize loads 12-slot inventory]
    O --> P{Inventory loaded?}
    P -- No --> P1[Show inventory unavailable message]
    P1 --> J
    P -- Yes --> Q[Open CustomerWindow]

    Q --> R{Customer activity}
    R -- Insert cash --> S[Increase inserted balance]
    R -- QR payment --> T[QrPaymentService creates payment intent]
    T --> U[QrPaymentWindow displays QR code]
    U --> V{Payment marked paid?}
    V -- No --> R
    V -- Yes --> W[Add paid amount to balance]
    R -- Recycle item --> X[Choose recyclable item and point amount]
    X --> Y[Add pending recycle points]
    R -- Select product --> Z{Enough balance and stock?}

    S --> R
    W --> R
    Y --> R
    Z -- No --> R
    Z -- Yes --> AA[Decrease product stock in memory]
    AA --> AB[DataStore.SaveInventory]
    AB --> AC{Current data source}
    AC -- Supabase --> AD[Patch machine_inventory stock]
    AC -- Local cache --> AE[Save dirty inventory locally]
    AD --> AF[Record sale and event log]
    AE --> AF
    AF --> AG[Build Transaction and receipt lines]
    AG --> AH[DataStore.SaveCompletedReceipt]
    AH --> AI{Current data source}
    AI -- Supabase --> AJ[Insert receipt_sessions and receipt_session_lines]
    AI -- Local cache --> AK[Queue receipt session for replay]
    AJ --> AL[Show ReceiptWindow and optional print]
    AK --> AL
    AL --> AM{Continue customer session?}
    AM -- Yes --> R
    AM -- No --> J

    J -- RFID scan --> AN[ArduinoService raises card scanned event]
    AN --> AO[MainWindow checks customers table through SupabaseStore]
    AO --> AP{RFID registered?}
    AP -- No --> AQ[Send prompt and open CustomerRegistrationWindow]
    AQ --> AR[Register customer in customers table]
    AR --> J
    AP -- Yes --> AS[Send valid response and open CustomerDashboardWindow]
    AS --> AT{Pending recycle points?}
    AT -- Yes --> AU[Save points to customer eco_credits]
    AT -- No --> J
    AU --> J

    J -- Admin button --> AV[Open LoginWindow]
    AV --> AW[SupabaseStore.AuthenticateUser]
    AW --> AX{Valid user?}
    AX -- No --> AY[Show login error]
    AY --> J
    AX -- Yes --> AZ[Open AdminWindow with role and assigned machine IDs]
    AZ --> AZ1{Inventory Manager?}
    AZ1 -- Yes --> AZ2[Show only assigned-machine Inventory view]
    AZ2 --> BA
    AZ1 -- No --> BA

    BA{Admin action}
    BA -- Dashboard --> BB[Load sales, stock, machine, and alert metrics]
    BA -- Items tab --> BC[Manage global catalog in items]
    BA -- Inventory tab --> BD[Assign items to machine slots in machine_inventory]
    BA -- Machines tab --> BE[Create, edit, delete vending_machines]
    BA -- Map picker --> BF[MapPickerWindow selects coordinates and address]
    BA -- Users tab --> BG[Manage users and roles]
    BA -- Recyclables tab --> BH[Manage recyclable_items]
    BA -- Sales/logs --> BI[Read sales_transactions and event_logs]
    BA -- Exit admin --> J

    BB --> BA
    BC --> BA
    BD --> BA
    BE --> BA
    BF --> BA
    BG --> BA
    BH --> BA
    BI --> BA

    J -- Exit app --> BJ([Application closes])
```

## How to Explain It

- Startup decides which data source is available.
- Customer mode uses `MachineSelectionWindow`, `DataStore`, `CustomerWindow`, and the sync layer.
- Admin mode uses `LoginWindow`, `AdminWindow`, and `SupabaseStore`.
- RFID scans can interrupt from the main screen and route into registration or dashboard.
- QR payment uses `QrPaymentService` and the Supabase Edge Function.
- Completed sales update inventory, event logs, sales history, and receipt history.
