# Database Connection Sequence Diagram

```mermaid
sequenceDiagram
    participant UI as WPF Window
    participant Store as SupabaseStore
    participant Client as SupabaseClient
    participant REST as Supabase PostgREST
    participant PG as PostgreSQL Tables

    UI->>Store: call app method, e.g. GetMachineInventory(1)
    Store->>Client: build table name and PostgREST query string
    Client->>REST: HTTP GET/POST/PATCH/DELETE with apikey + Bearer key
    REST->>PG: execute against public schema with RLS policies
    PG-->>REST: rows or write result
    REST-->>Client: JSON
    Client-->>Store: JsonArray or response text
    Store-->>UI: DataTable, model list, bool, or tuple
```

## How to Explain It

- The WPF windows do not directly open PostgreSQL connections.
- `SupabaseStore` exposes project-specific methods such as `GetMachineInventory`, `AddCatalogItem`, `RecordSale`, and `UpdateCustomerCredits`.
- `SupabaseClient` owns the HTTP client and reads `ECOMATIC_SUPABASE_URL` plus the Supabase API key from environment configuration.
- Supabase exposes the PostgreSQL database through PostgREST at `/rest/v1`.
- Normal table work uses HTTP methods: `GET` for reads, `POST` for inserts, `PATCH` for updates, and `DELETE` for deletes.
- QR payment work uses the active `qr-payment-confirm` Edge Function at `/functions/v1/qr-payment-confirm`.

