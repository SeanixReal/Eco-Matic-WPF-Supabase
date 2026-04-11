# Eco-Matic Vending Machine (WPF) 🌿

## Overview
Eco-Matic is a modern, object-oriented desktop application providing a comprehensive interface for simulating a smart vending machine. Built with C# and WPF (Windows Presentation Foundation), the app allows users to participate in mock purchasing and recycling flows while providing administrators a robust UI for inventory and event log management.

## Audience
This document serves as the top-level architectural map and instruction manual for both **Human Developers** and **AI Assistants**.

### For Humans 👨‍💻
*   **Framework:** .NET 10.0 (WPF)
*   **UI Paradigm:** MVVM (Model-View-ViewModel) loosely structured via Code-Behind. Focused heavily on custom XAML templating, control styling, and scalable UI elements.
*   **Persistence:** CSV File flat-file storage handled through `CsvStorage.cs`.
*   **Core Logic:** The global application state and default bootstrap exist inside `DataStore.cs`.

### For AI Agents 🤖
*   **Design Pattern:** Object-Oriented Programming (OOP) utilizing core concepts: **Inheritance, Abstraction, and Polymorphism**.
*   **Base Types:** `VendingItem.cs` is the core abstract class that `SnackItem`, `DrinkItem`, and `MiscItem` subclass. 
*   **Data Types:** 
    *   Do **NOT** attempt to use `Product` or unstructured structs.
    *   Rely on `is IHasCalories` or `is IHasVolume` interfaces for type-specific checks.
*   **Rule Engine:** Always rely on `DataStore.Products` for state manipulation rather than creating ghost copies unless explicitly executing cart transactions.
*   **Build Verification:** Do an `out-of-place` build when the `.exe` is locked using: `dotnet build -p:UseAppHost=false -p:OutDir=bin/verify/`

## Project Structure
*   **/Models/** - Domain Objects representing Inventory, Transactions, and Receipts.
*   **/Data/** - Abstraction for Data Loading (`CsvStorage.cs`) and State Management (`DataStore.cs`).
*   **/Utilities/** - Auxiliary toolsets like formatting or Image handling.
*   **/doc/** - All internal documentation (Flowcharts, Diagrams, Specifications).

## Developer Workflows
*   **Testing:** If modifying `CsvStorage.cs`, ensure backwards compatibility with `inventory.csv` layout. The format explicitly binds columns 7 & 8 to Calories and Volume using integer defaults.
*   **UI Changes:** To modify global theme assets, alter `App.xaml` resources. Window-specific structural changes should be made using WPF XAML constructs (e.g., `<Grid>`, `<StackPanel>`) avoiding absolute pixel positioning where responsive stretching is preferable.