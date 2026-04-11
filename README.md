# Eco-Matic Vending Machine Simulator (GUI Edition) 

## 1. Introduction

### 1.1 Project Overview
The "Eco-Matic" Simulator is a graphical application built with C# WPF (migrated from the originally proposed WinForms architecture for better modern UI capabilities). It promotes **SDG 12 (Responsible Consumption)** by integrating a standard vending machine with a unique "trash-to-credit" recycling system. This project moves beyond previous console constraints into a professional GUI with persistent data integration.

### 1.2 Objectives
- Develop an intuitive and modern WPF GUI for customer and admin interactions.
- Implement robust data persistence (via CSV structured schemas) to maintain inventory and states securely.
- Simulate a "Recycle for Credit" module for sustainable waste management.
- Enforce role-based security via an authenticated administrator dashboard.

### 1.3 Scope
The system includes a Customer UI for purchasing, recycling, and balance management, and an Administrator UI for inventory CRUD (Create, Read, Update, Delete) operations and transaction auditing.

## 2. System Analysis

### 2.1 Problem Statement
The previous CLI (Command Line Interface) version lacked user-friendly interaction and relied on linear script flows, making data management and visual presentation difficult.

### 2.2 Proposed Solution
A GUI C#.NET application provides visual feedback, ensures data integrity, and allows for efficient inventory and log management using strict Object-Oriented Programming (OOP) principles.

### 2.3 Feasibility Study
- **Technical:** Reuses core OOP logic from OOP1 while introducing advanced Data-Binding and separated UI layouts with XAML.
- **Operational:** GUI significantly improves usability over command-line inputs.
- **Financial:** Uses free and open-source tools (Visual Studio Community, .NET SDK).

## 3. System Design

### 3.1 System Architecture
The program operates on a **Presentation, Business Logic, and Data Access** tiered structure. This ensures a clean separation between the WPF UI (XAML/Code-Behind) and the underlying data storage logic (`DataStore.cs` and `CsvStorage.cs`).

- **Inheritance & Polymorphism:** Specific products are concrete subclasses (`SnackItem`, `DrinkItem`, `MiscItem`) inheriting from an abstract `VendingItem`, making the business logic heavily robust.

### 3.2 Modules
- **Auth:** Secure Admin login handling.
- **CRUD:** Full Inventory management allowing Admins to Add, Restock, or Remove `VendingItem` nodes.
- **Transactions:** Customer balance processing and recycling credit logic.

## 4. Implementation Plan

### 4.1 Development Tools & Technologies
- C#, WPF (Windows Presentation Foundation), .NET 10.0
- Visual Studio 2022 / Visual Studio Code
- Integrated Csv Data File Handling (Replacing initially proposed MySQL for zero-setup portability)

### Setup & Execution

1. Clone this repository:
  ```bash
  git clone https://github.com/SeanixReal/Eco-Matic-WPF.git
  cd Eco-Matic-WPF/Eco-Matic
  ```

2. Restore dependencies and build:
  ```bash
  dotnet restore
  dotnet build Eco-Matic.csproj
  ```

3. Run the app:
  ```bash
  dotnet run --project Eco-Matic.csproj
  ```

## 5. Storage & Database Design

While adapted to use a zero-configuration flat-file (CSV) system for extreme portability, it strictly mimics relational table logic:

### 5.1 Tables (CSV Structures)

**Inventory Table (`inventory.csv`)**
- `ProductId` (INT)
- `Type` (VARCHAR/ENUM - Snack, Drink, Misc)
- `Name` (VARCHAR)
- `Price` (DECIMAL)
- `Stock` (INT)
- `Extended Attributes` (Calories, Volume, etc.)

**Transaction Table (`eventLog.csv`)**
- `LogDate` (DATETIME)
- `ActionType` (VARCHAR)
- `Details` (TEXT)

## 6. Testing & Quality Assurance

- **Unit/Integration:** Testing OOP polymorphic state changes (e.g., verifying a `DrinkItem` registers its volume correctly).
- **User Acceptance Testing:** Verifying intuitive UI flow, error handling (e.g., purchasing without enough balance), and ensuring clean XAML layouts.
- **Outcome:** A stable, secure desktop app with resilient data tracking.

## 7. Conclusion

The Eco-Matic GUI Edition improves usability and reliability by transitioning from a console prompt to an event-driven WPF application. By properly storing states and applying advanced OOP architecture, it showcases professional data management and software development practices.

## 8. References

- Microsoft. (2024). WPF Documentation. https://learn.microsoft.com/en-us/dotnet/desktop/wpf/
- SeanixReal. (2023). Eco-Matic OOP1 Terminal Project. GitHub. https://github.com/SeanixReal/Eco-Matic
- United Nations. (n.d.). Sustainable Development Goal 12. https://sdgs.un.org/goals/goal12
