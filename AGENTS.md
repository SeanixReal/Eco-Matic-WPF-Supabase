# Eco-Matic Development Guide

## Build and Run Commands

- **Build**: `dotnet build`
- **Run**: `dotnet run --project Eco-Matic.csproj`
- **Clean**: `dotnet clean`

## Project Architecture

- **Framework**: .NET 10.0 WPF
- **Primary UI**: customer vending machine view plus admin management console
- **Data Access**: Supabase via `Data/SupabaseStore.cs` and `Data/SupabaseClient.cs`
- **Hardware Integration**: `Data/ArduinoService.cs` for RFID scanning and LCD/status messaging
- **Session State**: `Data/DataStore.cs` caches the active machine inventory during customer mode

## Canonical Documentation

Use `docs/` as the single source of truth.

- `docs/CODEBASE_ARCHITECTURE.md`
- `docs/CODE_REVIEW.md`
- `docs/MAINTAINER_GUIDE.md`
- `docs/PROFESSOR_ARCHITECTURE_GUIDE.md`
- `docs/USER_MANUAL.md`

## Important Current Realities

- The repo no longer uses `MySqlStore`; references to MySQL are historical unless they are explicitly about old migration files.
- The customer UI has **12 visible vending slots**.
- `DataStore.Initialize()` maps products to UI slots using the real normalized `slot_id`.
- Global item identity lives in `items`, while machine-specific stock and optional price overrides live in `machine_inventory`.
- The admin UI now separates:
  - global catalog management in the `Items` tab
  - machine slot assignment and stock management in the `Inventory` tab
- Canonical slot IDs are `1` through `12`; legacy `S1`-style slots are read-compatible only.
- RFID currently supports customer registration and saving recycle credits.
- Customer purchases are still cash-based in `CustomerWindow.xaml.cs`.
- Images are local-first and should continue to work without Supabase Storage.

## Coding Standards

- Keep UI layout and styling centralized in XAML resources when possible.
- Prefer `Grid` and `Border` for WPF structure.
- Use PascalCase for classes and methods, camelCase for local variables.
- Preserve the current MVVM-lite structure:
  - window behavior in `.xaml.cs`
  - reusable backend/hardware/image logic in service/helper classes

## Key Files

- `MainWindow.xaml.cs`: app entry flow, RFID routing, mode selection
- `CustomerWindow.xaml.cs`: customer vending logic
- `AdminWindow.xaml.cs`: admin dashboards, inventory, machines, users, sales, logs
- `Data/DataStore.cs`: in-memory customer session state
- `Data/SupabaseStore.cs`: main backend service
- `Data/SupabaseStore_Customers.cs`: RFID customer operations
- `Data/SupabaseClient.cs`: low-level Supabase REST client
- `Data/ArduinoService.cs`: serial hardware communication
- `Utilities/ImageLoader.cs`: resilient image loading
- `docs/CODE_REVIEW.md`: current logic review and known risks

## AI Agent Guidance

- Do not reintroduce `MySqlStore` assumptions into docs or code comments.
- When documenting architecture, describe the current Supabase-backed flow.
- Be careful when changing inventory logic:
  - global catalog fields in `items`
  - machine slot behavior in `machine_inventory`
  - customer 12-slot UI
  - admin `Items` vs `Inventory` workflows
  all need to stay aligned.
- Do not move product images to a cloud-only dependency unless offline behavior is intentionally redesigned.
- If touching authentication or customer storage, note that passwords are currently stored and compared directly; avoid silently documenting them as hashed when they are not.
