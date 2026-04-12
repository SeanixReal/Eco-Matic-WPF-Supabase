# Eco-Matic: Professor & Presentation Guide

This document is designed to help you (the student) explain your program to your professor or evaluator. It highlights the core computer science and Object-Oriented Programming (OOP) concepts utilized in the Eco-Matic project.

## 1. Object-Oriented Programming Principles
*   **Separation of Concerns (UI vs. Data Layer)**: 
    *   The project strictly separates the frontend (UI) from the backend (Database). Files like `AdminWindow.xaml.cs` **never** execute raw SQL queries. Instead, they call methods from `Data/MySqlStore.cs`. This encapsulation means the database technology could be swapped entirely without rewriting the UI.
*   **Encapsulation & Abstraction**: 
    *   Complex database logic, like calculating metrics or processing event logs, is hidden behind simple method calls like `store.GetDashboardMetrics(...)`. The UI doesn't need to know *how* the data is fetched, just *what* data it receives.

## 2. System Architecture & Performance
*   **Single-Window Application (SPA pattern)**: 
    *   Instead of creating and destroying new windows for every section (Inventory, Sales, Logs, etc.), the application uses a single `AdminWindow` and dynamically toggles the `Visibility` of WPF Grids. 
    *   *Why this matters*: It drastically reduces memory overhead and CPU usage, providing a significantly faster and smoother user experience compared to traditional multi-window desktop apps.
*   **In-Memory Data Slicing (`System.Data.DataView`)**:
    *   When an Inventory Manager logs in, they only see machines assigned to them. Instead of hitting the database with multiple distinct queries, a single block of data is fetched and filtered in RAM using C#'s `DataView.RowFilter`. This minimizes expensive network/database round-trips.

## 3. Security & Features
*   **Role-Based Access Control (RBAC)**:
    *   The system actively verifies the `_currentUserRole`. If the user is an "Inventory Manager", the system automatically restricts access by hiding the Dashboard, Sales, and User management tabs.
*   **Data Integrity (SQL Constraints)**:
    *   The C# application acts as a safeguard. For example, before adding a new item, `AddNewItemToMachine` checks a `COUNT(*)` to ensure the physical limits of the vending machine (12 slots) are strictly adhered to, rejecting the transaction if it breaches physical capacities.

## Presentation Tip:
When presenting your code, open `AdminWindow.xaml.cs` and `MySqlStore.cs` side-by-side. 
Show how a button click in `AdminWindow` easily triggers a complex sequence of SQL commands in `MySqlStore`, proving you understand how the two layers communicate!