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

The current system is **not** a true offline-first application.

- It reads and writes through live Supabase REST calls.
- `DataStore` only keeps in-memory state for the active vending session.
- There is no persisted local database snapshot plus delayed sync queue yet.

That means:

- if the app already loaded a machine inventory and then the internet drops, some in-session UI behavior may continue temporarily
- but full offline startup, reliable offline transactions, and automatic replay to Supabase when Wi-Fi returns are **not implemented yet**

## Migration Note

If your Supabase database was created before the per-machine price override refactor, run `docs/migration_increment3.sql`.

That migration adds `machine_inventory.slot_price` and normalizes legacy slot IDs like `S1` into canonical values like `1`.

## Documentation

Project documentation now lives in `docs/`.

- `docs/CODEBASE_ARCHITECTURE.md`
- `docs/DIAGRAMS.md`
- `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`
