# Eco-Matic: Developer & AI Guide

This document serves as the central reference point for AI assistants, software engineers, and system architects modifying or adding features to this repository.

## 1. Project Architecture
The project is built on ASP.NET Core 10 Windows Presentation Foundation (WPF) using C#. 
It uses a 2-tier architecture (Client -> Database), connecting directly to a local or remote MySQL Server (`MySql.Data`).

### Key Directories
*   `Eco-Matic/`: The core project directory. Let's keep all active code here.
*   `Eco-Matic/Data/`: Contains the Data Access Layer `MySqlStore.cs`. This is where all SQL strings and MySqlConnection objects live.
*   `Eco-Matic/Models/`: Contains runtime objects and entities for use across the application (`Product.cs`, `Transaction.cs`, `VendingItem.cs`).

## 2. Admin UI & Event-Driven Mechanics
The `AdminWindow.xaml.cs` file utilizes an Event-Driven paradigm, managing a simulated SPA (Single-Page Application).
*   **View Toggling**: `SetActiveView(string viewName)` switches between grids (`viewDashboard`, `viewInventory`, `viewSales`) via `Visibility.Visible` and `Visibility.Collapsed`.
*   **Never embed SQL queries in XAML code-behinds.** Always write an interfacing wrapper method in `Data/MySqlStore.cs` and invoke it from the UI class.

## 3. Database Layer (`MySqlStore.cs`)
*   **Database Engine**: MySQL.
*   **Connection**: All transactions should open and dispose of connections properly using `using var conn = GetConnection();` followed by `conn.Open();`.
*   **Parameters**: Always use parameterized queries (`@paramName`) to prevent SQL Injection, especially when handling `AdminWindow` inputs or `CustomerWindow` purchases.

## 4. Notable Constraints
*   `machine_inventory`: Maximum of 12 distinct items allowed per connected `machine_id`. The C# `AddNewItemToMachine()` method enforces this via `SELECT COUNT(*)`. 
*   `vending_machines`: Hardcoded maximum of 4 machines allowed globally. 

## 5. Randomization & Mock Data
If a user requires mock data to populate slots for a demo:
*   Do not inject randomization directly into the application runtime or startup sequence (`DataStore.Initialize`).
*   Instead, run raw SQL to modify `stock_level` directly against the database:
    `UPDATE machine_inventory SET stock_level = FLOOR(1 + (RAND() * 15));`