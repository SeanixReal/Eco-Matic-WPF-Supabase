# Eco-Matic Database Migration & RBAC Implementation Log

## 1. Overview
This document serves to explain the structural changes applied to the Eco-Matic Vending Machine system for the transition from mock CSV data to a localized MySQL database schema. It also covers the shift to Role-Based Access Control (RBAC) and new UI flows.

## 2. Rationale
The initial version of the project used flat `CSV` files for simple mocking. However, moving to a normalized `MySQL` database enables:
- **Relational tracking**: Linking sales to specific machine instances and items.
- **RBAC (Role-Based Access Control)**: Enforcing secure administrative environments where an `Admin` sees everything, but an `Inventory Manager` only has stock permissions.
- **Scalability**: Supports multiple abstract vending machines (up to 4) under a global catalog network.

## 3. Database Schema Design (ecomatic_db)
Located in `docs/database_setup.sql`. The core entities are:
- `roles` & `users`: Establishes the RBAC system, locking down credentials safely.
- `vending_machines`: Handles up to 4 parallel operating vending machines.
- `items`: Acts as the *global catalog*, identifying the base configurations for any item sold across all machines.
- `machine_inventory`: The junction table linking the catalog to a machine and storing current capacity attributes.
- `sales_transactions` & `event_logs`: Time-series ledger tables for robust logging available on the Admin Dashboard.

## 4. UI/UX Flow Modifications
- **Startup**: `MainWindow.xaml` remains the unauthenticated starting hub.
- **Customer Route**: Clicking "Customer Panel" now pushes the user to a `MachineSelectionWindow`, identifying which physical machine they are simulating before launching `CustomerWindow`.
- **Admin Route**: Clicking "Admin Panel" launches a secure `LoginWindow` connecting directly to the MySQL users table. 
- **Admin Layout**: Replaces the generic stack format with a Sidebar Navigation Dashboard containing restricted tabs to enforce RBAC cleanly.

## 5. Security Decisions
- Native Windows title bars are removed (`WindowStyle="None"`) strictly for aesthetic congruence. Window movement relies on a custom `MouseLeftButtonDown` event loop routing.
- The RBAC logic assumes the DB dictates the truth. Depending on the `role_id` resolving upon a successful login event, the C# logic determines which UI elements become visible.

Prepared on April 12, 2026.
## 6. Hardware & RFID Loyalty System Integration
- **Trash-to-Credit Program**: We implemented an RFID-based loyalty program where users scan an RFID tag to link purchases to an e-wallet balance (`eco_credits`).
- **Arduino Hardware Interface**:
  - Utilizing the `System.IO.Ports.SerialPort` in C# to establish a persistent physical connection on `COM5` at 9600 baud rate.
  - The PC acts as the master, listening asynchronously to RFID signals and invoking UI updates on the main WPF `Dispatcher`.
- **Bidirectional Communication Framework**:
  - The C# application sends commands like `STATE:AFK` or `STATE:ACTIVE` to control the Arduino's behavior.
  - When the machine is idle, the Arduino displays screensaver statistics ("Fun Facts") respecting the 16x2 I2C LCD character bounds, bypassing the RFID scanner to save power.
  - Upon an RFID scan, the UID is sent to the PC, checked against the `customers` database, and either replies with `VALID` (Access Granted) or `INVALID` (Registration Flow Trigger).
- **Admin CRM Module**: The Admin Control Panel includes a "Customers" tab with full CRUD capability for the `customers` table to manually adjust loyalty points.
