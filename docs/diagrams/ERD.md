# Entity Relationship Diagram

This ERD reflects the live Supabase public schema verified through the Supabase MCP tool on 2026-04-26.

```mermaid
erDiagram
    ROLES ||--o{ USERS : assigns
    VENDING_MACHINES ||--o{ USERS : optional_assignment
    USERS ||--o{ USER_MACHINE_ASSIGNMENTS : has_inventory_scope
    VENDING_MACHINES ||--o{ USER_MACHINE_ASSIGNMENTS : assigned_to_staff
    VENDING_MACHINES ||--o{ MACHINE_INVENTORY : contains
    ITEMS ||--o{ MACHINE_INVENTORY : stocked_in
    VENDING_MACHINES ||--o{ SALES_TRANSACTIONS : records
    ITEMS ||--o{ SALES_TRANSACTIONS : sold_as
    VENDING_MACHINES o|--o{ EVENT_LOGS : logs_for
    VENDING_MACHINES ||--o{ RECEIPT_SESSIONS : issues
    RECEIPT_SESSIONS ||--o{ RECEIPT_SESSION_LINES : contains
    RECYCLABLE_ITEMS o|--o{ RECEIPT_SESSION_LINES : describes_recycle_line
    VENDING_MACHINES ||--o{ ESP32_TELEMETRY : reports
    VENDING_MACHINES ||--o{ ESP32_COMMANDS : receives

    ROLES {
        int role_id PK
        string role_name UK
    }

    USERS {
        int user_id PK
        string username UK
        string password_hash
        int role_id FK
        int assigned_machine_id FK "legacy primary assignment"
        timestamptz created_at
    }

    USER_MACHINE_ASSIGNMENTS {
        int user_id PK,FK
        int machine_id PK,FK
        timestamptz created_at
    }

    VENDING_MACHINES {
        int machine_id PK
        string location_name
        string status
        timestamptz created_at
        text address_text
        float latitude
        float longitude
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
        timestamptz created_at
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
        timestamptz transaction_date
        uuid client_sync_id
    }

    EVENT_LOGS {
        int log_id PK
        string event_type
        text description
        int machine_id FK
        timestamptz log_date
        uuid client_sync_id
    }

    CUSTOMERS {
        string rfid_tag PK
        string email UK
        string password_hash
        int eco_credits
        timestamptz registered_date
    }

    RECEIPT_SESSIONS {
        bigint receipt_session_id PK
        uuid client_sync_id UK
        string receipt_number
        int machine_id FK
        timestamptz session_started_at
        timestamptz session_ended_at
        decimal total_amount
        decimal amount_paid
        decimal change_amount
        int recycle_points_total
        string source
        timestamptz created_at
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
        int recycle_item_id FK
        string recycle_display_name
        string recycle_material_type
        string recycle_unit_label
        int recycle_points_per_unit
    }

    RECYCLABLE_ITEMS {
        int id PK
        string display_name
        string material_type
        string unit_label
        int points_per_unit
        text description
        bool is_active
        int sort_order
        timestamptz created_at
    }

    QR_PAYMENT_INTENTS {
        bigint id PK
        text reference UK
        text token
        int machine_id "logical reference, no FK"
        decimal amount
        text status
        timestamptz created_at
        timestamptz scanned_at
        timestamptz paid_at
    }

    ESP32_TELEMETRY {
        int telemetry_id PK
        int machine_id FK
        string device_id
        decimal temperature
        decimal humidity
        bool door_open
        string power_status
        timestamptz recorded_at
    }

    ESP32_COMMANDS {
        int command_id PK
        int machine_id FK
        string command_type
        jsonb payload
        string status
        timestamptz created_at
        timestamptz executed_at
    }
```

## How to Explain It

- `items` is the global product catalog.
- `machine_inventory` is the per-machine slot table for stock, capacity, and optional slot price.
- `vending_machines` is the parent for inventory, staff machine assignments, sales, event logs, telemetry, ESP32 commands, and receipt sessions.
- `receipt_sessions` and `receipt_session_lines` store complete receipt history.
- `customers` stores RFID customers and eco-credit balances. It is not currently connected to sales by a foreign key.
- `user_machine_assignments` lets one inventory manager manage multiple assigned vending machines. `users.assigned_machine_id` remains as a legacy primary assignment for compatibility.
- `qr_payment_intents` supports QR payment through the `qr-payment-confirm` Edge Function. Its `machine_id` is a nullable logical reference in the current live schema, not an enforced foreign key.

Use this defense line:

> A product can exist globally once, but stock belongs to a specific vending machine slot, so stock must live in `machine_inventory`, not in `items`.
