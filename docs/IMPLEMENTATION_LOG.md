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