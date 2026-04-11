# Eco-Matic Vending Machine: System Flow Chart

```mermaid
graph TD
    A[Start App] --> B{DataStore.Initialize()}
    B --> C[Load inventory.csv via CsvStorage]
    C --> D[Show MainWindow]
    
    D -->|Click Customer| E[CustomerWindow]
    D -->|Click Admin| F[Login Dialog]
    D -->|Click Exit / Close| X[End App]
    
    %% Customer Flow
    E --> C1(Insert Money)
    E --> C2(Insert Recyclables)
    E --> C3(Select Product)
    E --> C4(Get Change / Exit)
    
    C2 --> C2a[Calculate Credit]
    C2a --> C_State((Customer State))
    
    C1 --> C_State
    C3 --> C_Check{Sufficient Funds?}
    
    C_Check -->|Yes| C_Dispense[Dispense Item, Deduct Stock]
    C_Dispense --> C_Log[Log Event to CSV]
    C_Log --> C_State
    C_Check -->|No| C_State
    
    C4 --> C_Print[Print Receipt Modal]
    C_Print --> D
    
    %% Admin Flow
    F --> A_Check{Password Match?}
    A_Check -->|No| F_Deny[Show Error]
    F_Deny --> D
    A_Check -->|Yes| A_Win[AdminWindow]
    
    A_Win --> A1(View Events)
    A_Win --> A2(Restock Items)
    A_Win --> A3(Add New Item)
    A_Win --> A4(Remove Item)
    
    A2 --> A_Save[DataStore.SaveInventory()]
    A3 --> A_Save
    A4 --> A_Save
    A_Save --> A_Win
    
    A_Win -->|Close| D
```
