# Eco-Matic Vending & Recycling System

## Overview
Eco-Matic is a complete C# WPF point-of-sale and "Trash-to-Credit" loyalty system integrated with a physical Arduino RFID/LCD hardware module. The project allows users to purchase items, while simultaneously dropping off recyclables (bottles, cans) to earn Eco-Credits.

## Features
- **Vending & Inventory Management**: Full WPF graphical interface for managing machines, stock, and purchasing catalog items.
- **Hardware Integration (Arduino)**:
  - Uses an Arduino Uno/Nano, RC522 RFID reader, 16x2 I2C LCD display, and green/red LEDs.
  - Bidirectional USB Serial communication defaults to `COM5` at `9600` baud to handle physical hardware states, LCD messages, RFID scans, and LED feedback.
- **Eco-Credits Loyalty Program**: 
  - Scan physical RFID cards to register/login.
  - E-Wallet dashboard for tracking accumulated points.
  - Save recycle points to an RFID-linked customer account.
- **Admin CRM**: 
  - Role-Based Access Control (RBAC).
  - Customer relation management backed by Supabase to modify or view registered users and point balances.
- **Event Logging**: Time-based filtering (Day, Week, Month) of all machine, sales, and user events for auditing.

## Setup
- **Database**: The current application uses Supabase via `Data/SupabaseStore.cs` and `Data/SupabaseClient.cs`. SQL reference files are organized under `docs/sql/`.
- **Hardware**: Wire the Arduino hardware per `Arduino/README.md` and flash `Arduino/RFID_Scanner/RFID_Scanner.ino`.
- **Application**: Open the project in Visual Studio and build it, or run `dotnet run` in the root folder.

## Environment Setup

Eco-Matic now requires a repo-root `.env` file before startup.

1. Copy `.env.example` to `.env`
2. Fill in your real Supabase URL/key
3. Launch the app after the Supabase project is reachable

Important notes:

- the app will fail fast on startup if `.env` is missing or still contains placeholder values
- `.env.example` is the tracked template, while `.env` stays local and ignored
- if you also set the same variables in Windows, those OS values win over `.env`
- optional hardware settings are `ECOMATIC_ARDUINO_PORT` and `ECOMATIC_ARDUINO_BAUD`

## Connectivity Behavior

The current system is Supabase-first and requires live Supabase connectivity for customer and admin data.

- customer vending mode reads machine lists and inventory from Supabase
- purchases, stock updates, event logs, receipts, RFID registration, and credit updates write to Supabase
- if Supabase is unreachable, customer/admin data features show a connectivity message instead of using a local database fallback

## Migration Note

If your Supabase database was created before the per-machine price override refactor, run `docs/sql/migrations/supabase/migration_increment3.sql`.

That migration adds `machine_inventory.slot_price` and normalizes legacy slot IDs like `S1` into canonical values like `1`.

If your live schema is older and does not have `client_sync_id` support on sales/event tables, run `docs/sql/migrations/supabase/migration_increment4.sql`.

For the live schema audit and the current authentication/RLS findings, see `docs/SUPABASE_AUDIT.md`.

## Documentation

Project documentation now lives in `docs/`.

- `docs/CODEBASE_ARCHITECTURE.md`
- `docs/DIAGRAMS.md`
- `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`
