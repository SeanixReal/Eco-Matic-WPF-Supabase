# Eco-Matic Supabase Migration Guide

## Overview
The Eco-Matic WPF project was migrated from a local MySQL database to a cloud-based **Supabase (PostgreSQL)** backend. The current backend supports remote access, easier classroom/demo setup, and future IoT integration without requiring a self-hosted MySQL server.

## Architecture Changes
### 1. Removal of `MySql.Data`
The `MySql.Data` dependency has been removed from the project.

### 2. Lightweight REST API Data Access Layer
Instead of maintaining a persistent database socket connection, the application now uses REST API calls through `System.Net.Http.HttpClient`.

**Key Classes Introduced:**
- `Data/SupabaseClient.cs`: A wrapper for `HttpClient` that handles the authentication headers and endpoints for the Supabase PostgREST API.
- `Data/SupabaseStore.cs` and `Data/SupabaseStore_Customers.cs`: the application-level replacement for the old `MySqlStore`. The store exposes mostly synchronous methods so the existing WPF event-handler structure can continue to work while the lower-level HTTP calls remain asynchronous.

## Database Schema
The database uses Row-Level Security (RLS) with project/demo policies that allow the application to read and write data using the configured Supabase API key.

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
Because the system now relies on Supabase's REST API, ESP32 integration can use standard HTTP requests instead of a full SQL client library.

### Note on IoT integration
The code to operate the ESP32 has been moved to a dedicated file: `Data/Esp32SupabaseClient.ino`.

## Security Note
The app currently uses a Supabase anon/publishable key from environment configuration. The live policies are permissive for project/demo use, so a production version should use fresh environment-specific keys and tighten Row Level Security policies before deployment.
