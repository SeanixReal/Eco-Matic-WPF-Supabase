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
    H --> I[Save inventory through OfflineSyncCoordinator]
    I --> J[Write or queue sale, event log, and receipt session]
    J --> K[Show dispense feedback and receipt]
    K --> L[Return remaining change]
    L --> M[End session]
```

## How to Explain It

- `DataStore.Initialize()` loads the chosen machine inventory.
- `CustomerWindow` handles balance checking, product selection, stock validation, and receipt display.
- `DataStore.SaveInventory()`, `DataStore.RecordSale()`, and `DataStore.SaveCompletedReceipt()` persist the customer action through the sync layer.
- Depending on connectivity, customer-mode writes go directly to Supabase or through the local queue.

