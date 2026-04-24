# Eco-Matic Supabase Migration Guide

## Overview
The Eco-Matic WPF project has been successfully migrated from a local MySQL database to a cloud-based **Supabase (PostgreSQL)** backend. This migration was performed to ensure the system is future-proof, highly accessible, and ready for integration with IoT devices (like the ESP32) without requiring complex networking or paid MySQL cloud hosting.

## Architecture Changes
### 1. Removal of `MySql.Data`
The heavy `MySql.Data` dependency has been completely removed from the project. 

### 2. Lightweight REST API Data Access Layer
Instead of maintaining a persistent, stateful connection to the database (which is error-prone and heavy), the application now uses a lightweight, stateless REST API architecture via the `System.Net.Http.HttpClient`.

**Key Classes Introduced:**
- `Data/SupabaseClient.cs`: A wrapper for `HttpClient` that handles the authentication headers and endpoints for the Supabase PostgREST API.
- `Data/SupabaseStore.cs` & `Data/SupabaseStore_Customers.cs`: A complete, drop-in replacement for the old `MySqlStore`. To ensure the existing WPF UI didn't break or require a massive `async/await` rewrite, the SupabaseStore uses a synchronous wrapper `Run()` over the asynchronous HTTP tasks.

## Database Schema
The database uses Row-Level Security (RLS) to ensure security while allowing the application to read and write data using the public `publishable` key.

The following tables exist in the `public` schema:
- `roles`
- `users`
- `vending_machines`
- `items`
- `machine_inventory`
- `customers`
- `sales_transactions`
- `event_logs`
- **`esp32_telemetry`** (Prepared for IoT)
- **`esp32_commands`** (Prepared for IoT)

## Future ESP32 IoT Integration
Because the system now relies on Supabase's REST API, connecting the ESP32 microcontroller is incredibly simple. You do not need a massive SQL library on the ESP32. You only need the standard `HTTPClient` library.

### Note on IoT integration
The code to operate the ESP32 has been moved to a dedicated file: `Data/Esp32SupabaseClient.ino`.

## Security Note
The `anon` key used in `SupabaseClient.cs` is safe to distribute within the client application because Supabase uses Row Level Security (RLS). Currently, the policies are set up to be highly permissive for development. Before moving to a production environment, ensure you lock down the RLS policies in the Supabase Dashboard.
