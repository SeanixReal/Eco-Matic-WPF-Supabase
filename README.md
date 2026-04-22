# Eco-Matic Vending & Recycling System

## Overview
Eco-Matic is a complete C# WPF point-of-sale and "Trash-to-Credit" loyalty system integrated with a physical Arduino-based RFID scanner. The project allows users to purchase items, while simultaneously dropping off recyclables (bottles, cans) to earn Eco-Credits.

## Features
- **Vending & Inventory Management**: Full WPF graphical interface for managing machines, stock, and purchasing catalog items.
- **Hardware Integration (Arduino)**: 
  - Uses an Arduino Uno, MFRC522 RFID reader, and 16x2 I2C LCD display.
  - Bidirectional USB Serial communication on `COM5` to handle physical hardware states (Active vs. AFK mode) and validation feedback.
- **Eco-Credits Loyalty Program**: 
  - Scan physical RFID cards to register/login.
  - E-Wallet dashboard for tracking accumulated points.
  - Save recycle points to an RFID-linked customer account.
- **Admin CRM**: 
  - Role-Based Access Control (RBAC).
  - Customer relation management backed by Supabase to modify or view registered users and point balances.
- **Event Logging**: Time-based filtering (Day, Week, Month) of all machine, sales, and user events for auditing.

## Setup
- **Database**: The current application uses Supabase via `Data/SupabaseStore.cs` and `Data/SupabaseClient.cs`. Historical SQL reference files are kept in `docs/`.
- **Hardware**: Wire the Arduino and MFRC522 per `Arduino/README.md` instructions and flash `RFID_Scanner.ino`.
- **Application**: Open the project in Visual Studio and build it, or run `dotnet run` in the root folder.

## Offline Behavior

The current system now supports **customer-mode offline caching and replay** after one successful online sync.

- customer vending mode reads machine lists and inventory from a local MySQL cache
- offline purchases update that local cache first and queue sales/logs for later replay
- when internet returns, queued writes replay to Supabase and the local cache refreshes again

Important limits:

- admin mode is still online-only
- RFID registration and RFID credit saving are still online-only
- the very first offline demo still requires one earlier successful online sync

## Migration Note

If your Supabase database was created before the per-machine price override refactor, run `docs/migration_increment3.sql`.

That migration adds `machine_inventory.slot_price` and normalizes legacy slot IDs like `S1` into canonical values like `1`.

If you want offline replay safety for customer mode, also run `docs/migration_increment4.sql`.

For the live schema audit and the current authentication/RLS findings, see `docs/SUPABASE_AUDIT.md`.

## Documentation

Project documentation now lives in `docs/`.

- `docs/CODEBASE_ARCHITECTURE.md`
- `docs/DIAGRAMS.md`
- `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`
