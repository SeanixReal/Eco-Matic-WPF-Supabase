# Eco-Matic System Evolution: Console vs. WPF

This document highlights the major structural, architectural, and design differences between the original `Eco-Matic-Console` application and the new `Eco-Matic-WPF` (GUI) application.

## 1. User Interface (UI) vs. Command Line Interface (CLI)

**Console:** 
*   **Interaction:** Text-based. Relied entirely on `Console.WriteLine` and `Console.ReadLine()`.
*   **Flow:** Linear and synchronous. The user was prompted step-by-step in a continuous while-loop.
*   **Visuals:** ASCII art or plain text menus.

**WPF:**
*   **Interaction:** Event-driven Graphical User Interface. Users click buttons, select items from grids, and type into customized text boxes.
*   **Flow:** Asynchronous and stateless from the user's perspective. The application waits idly for an event (like `Button_Click`) to trigger behavior.
*   **Visuals:** Modern, animated, borderless window with vector (`Path`) custom logos, drop shadows, rounded corners, and dynamic data binding using XAML.

## 2. Object-Oriented Principles (OOP) & Architecture

**Console:**
*   **Products:** Relied heavily on simple data structures, often utilizing single classes (like a `Product` class) coupled with basic `enum` discriminators (`ProductType.Snack`).
*   **Polymorphism:** Very limited. The system relied heavily on `if/switch` statements checking the enum to determine behavior instead of utilizing overridden methods or interface contracts.

**WPF:**
*   **Abstraction:** Introduces an abstract `VendingItem` class containing shared logic (`Name`, `Price`, `Stock`, `ProductId`).
*   **Inheritance:** Specific products are now concrete subclasses: `SnackItem`, `DrinkItem`, and `MiscItem`, each inheriting from `VendingItem`.
*   **Interfaces:** Implements specific contracts like `IHasCalories` (for snacks/drinks) and `IHasVolume` (for drinks), allowing for distinct property assignment depending on the object type.
*   **Polymorphism:** The `DataStore` manages a `List<VendingItem>`. Specific behaviors (like overriding the `Type` string or downcasting for UI display via pattern matching in C# 10+) are explicitly defined in the class hierarchy.

## 3. Data Persistence & State Management

**Console:**
*   **State:** Mostly held in memory during execution. If the console closed, data was often reset or saved synchronously in a blocking manner during shutdown.
*   **File I/O:** Simple text or JSON dumps, but deeply coupled with the console output mechanisms.

**WPF:**
*   **State (`DataStore.cs`):** Utilizes a centralized static/singleton pattern. This separates the logic from the UI (Code-Behind).
*   **Flat File Persistence (`CsvStorage.cs`):** Dedicated classes manage reading/writing. It specifically handles instantiating polymorphic subclasses during deserialization (e.g., checking if a CSV row explicitly belongs to a `DrinkItem` vs. `SnackItem`) using pattern matching (`item is IHasCalories`).
*   **Event Logging:** Introduces a decoupled tracking system mapping specific timestamps to events.

## 4. UI Implementation (XAML vs. Code-Behind)

**Console:**
*   No separation of concerns. The code that writes to the screen also processes the business rule check.

**WPF:**
*   **XAML:** Defines the view (Layouts, Grids, Borders, Brushes). It completely isolates *how* things look from *what* they do.
*   **Code-Behind (`.xaml.cs`):** Acts as a minimalist controller reacting to UI events (e.g., `AddToCart_Click`) and asking the domain model (`DataStore`) to perform the business logic.

## 5. Extensibility

**Console:** Adding a new product type (like `FrozenItem`) required modifying several `switch` statements scattered across logging, displaying, and selling logic.

**WPF:** Adding a new `FrozenItem` requires creating a new subclass (`public class FrozenItem : VendingItem`). The core `DataStore` list automatically handles it. You only need to update the specific CSV serialization parsing and the `Admin` add-item switch list.