# Entity Relationship Diagram

This ERD reflects the live Supabase public schema verified on 2026-04-30 and rechecked during the final presentation audit. It was updated on 2026-05-01 for the application-level receipt point-accounting update and the `items` soft-delete catalog design in migration increment 10. The 2026-05-14 README hardware GIF update documents customer/AFK Arduino states only and does not add database entities or relationships.

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
        bool is_active
        timestamptz deleted_at
        text deleted_reason
        timestamptz created_at
    }

    MACHINE_INVENTORY {
        int inventory_id PK
        int machine_id FK
        int item_id FK
        string slot_id
        int stock_level
        int max_capacity
        decimal slot_price "machine item price override"
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
        decimal amount_paid "cash/QR paid by customer"
        decimal change_amount
        int recycle_points_total "points earned from recycle lines"
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

```

## How to Explain It

- `items` is the global product catalog.
- `machine_inventory` is the per-machine slot table for stock, capacity, and optional machine item price override.
- Catalog deletion clears matching `machine_inventory` rows, then soft-deletes the `items` row by setting `is_active = false`, `deleted_at`, and `deleted_reason`. This keeps `sales_transactions` historical joins intact.
- `vending_machines` is the parent for inventory, staff machine assignments, sales, event logs, and receipt sessions.
- `receipt_sessions` and `receipt_session_lines` store complete receipt history.
- Receipt point usage is currently reflected in the application `Transaction` object, receipt screen, printed receipt, and purchase event logs. The live `receipt_sessions` table still stores cash/QR paid amount and recycle points earned, but it does not yet have dedicated persisted columns for points spent or final RFID balance.
- `customers` stores RFID customers and eco-credit balances. It is not currently connected to sales or event logs by a foreign key.
- Customer account transaction history is implemented at the application layer by reading purchase-related `event_logs.description` entries that include the RFID tag.
- Current-session purchase history can be attached to the first RFID scanned for that vending session, but this is still application-layer event-log behavior rather than a database foreign key.
- `user_machine_assignments` lets one inventory manager manage multiple assigned vending machines. `users.assigned_machine_id` remains as a legacy primary assignment for compatibility.
- `qr_payment_intents` supports QR payment through the `qr-payment-confirm` Edge Function. Its `machine_id` is a nullable logical reference in the current live schema, not an enforced foreign key.
- The 2026-05-01 foreground warning dialog and receipt point-usage fixes are application/UI changes and do not add database relationships shown here.
- The 2026-05-14 customer hardware view and AFK mode GIF additions are documentation/media updates for serial-driven hardware states, so the ERD remains unchanged.
- The `add_missing_foreign_key_indexes` migration adds indexes to existing foreign-key columns only. It improves lookup/delete performance but does not add entities or relationships to the ERD.

Use this defense line:

> A product can exist globally once, but stock belongs to a specific vending machine slot, so stock must live in `machine_inventory`, not in `items`.
