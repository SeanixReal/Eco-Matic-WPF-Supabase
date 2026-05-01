# Customer Buying Process Flowchart

```mermaid
flowchart TD
    A[Start customer session] --> B[Select vending machine]
    B --> C[DataStore initializes active machine inventory]
    C --> D[Customer inserts cash, scans QR payment, or earns recycle points]
    D --> E{Enough balance for item?}
    E -- No --> D
    E -- Yes --> F[Customer selects product]
    F --> G{Item in stock?}
    G -- No --> F
    G -- Yes --> H[Decrease in-memory stock]
    H --> I[Save inventory through SupabaseSessionCoordinator]
    I --> J[Write sale, event log, and receipt session to Supabase]
    J --> K[Show dispense feedback and receipt]
    K --> L[Return remaining change]
    L --> M[End session]
```

## How to Explain It

- `DataStore.Initialize()` loads the chosen machine inventory.
- `CustomerWindow` handles balance checking, product selection, stock validation, and receipt display.
- `DataStore.SaveInventory()`, `DataStore.RecordSale()`, and `DataStore.SaveCompletedReceipt()` persist the customer action through the Supabase session layer.
- Customer-mode writes go directly to Supabase; there is no local database fallback in the current build.
