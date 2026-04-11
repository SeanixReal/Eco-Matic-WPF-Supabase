# Eco-Matic: Eco Vending Machine WPF Application

## I. Project Title

**Eco-Matic** (Eco vending machine using WPF Application in C#)

## II. Introduction

This project aims to develop a fully-featured **"Eco-Matic" Vending Machine Simulator** with a Graphical User Interface (GUI) in C# WPF. 
It represents an evolution from the original console-based application, fully integrating modern Object-Oriented Programming (OOP) principles, multi-file architecture, and robust file handling.

The idea came from not just for commercial purposes but also to integrate a "trash to credit" recycling system, promoting awareness of Sustainable Development Goals (SDGs) like responsible consumption (SDG 12). 
This project can potentially be a great idea in the future to be put on the streets of Cebu to help clean the trashes. The program will be **data-driven**, with a **dynamic inventory** managed through a CSV file and a transaction log, updating instantly in the modern UI.

## III. Objectives

- **Develop a modern and interactive WPF GUI application** with clear, separated UI windows for customers and administrators.
- **Demonstrate inheritance and polymorphism** by creating an abstract `VendingItem` class with specialized child classes (`DrinkItem`, `SnackItem`, `MiscItem`) that provide unique behaviors.
- **Implement a data-driven inventory** that loads and saves its polymorphic state from a CSV file via a centralized `DataStore` pattern.
- **Create an admin panel**, protected by a login, for managing the machine's inventory tracking dynamically.
- **Integrate a "Recycle for Credit" feature**, allowing users to convert simulated trash (plastic, glass, aluminum) into usable machine credit.
- **Enforce realistic constraints**, combined with dynamic data binding to update the UI visuals (like stock warnings or error messages) automatically.

## IV. Scope

### What's Included:
- **Customer Transactions**: Simulates customer interactions (purchasing, recycling) seamlessly through button clicks and visual interactions.
- **Dynamic Inventory Management**: Real-time inventory updates with CSV persistence mapped directly to domain models.
- **Administrative Functions**: Managing inventory via dedicated grid layouts and forms.
- **WPF-based UI**: Modern, borderless window styling, custom vector graphics, drop shadows, and animated transitions.

## V. Project Requirements

### Software Requirements:
- **IDE**: Visual Studio 2022 or Visual Studio Code
- **Language**: C# 10+
- **Framework**: .NET 10.0 (WPF `net10.0-windows`)
- **Tools/Libraries**: 
  - Standard .NET WPF presentation frameworks (XAML)
  - `System.IO` for file handling and `CsvStorage` mechanisms
  - `System.Linq` for data manipulation

### Hardware Requirements:
- Any modern Windows computer capable of running the .NET runtime and WPF Desktop Framework.

### Setup & Execution

1. Clone this repository:

  ```bash
  git clone https://github.com/SeanixReal/Eco-Matic-WPF.git
  cd Eco-Matic-WPF/Eco-Matic
  ```

2. Install the .NET 10 SDK

3. Restore dependencies and build:

  ```bash
  dotnet restore
  dotnet build Eco-Matic.csproj
  ```

4. Run the WPF app:

  ```bash
  dotnet run --project Eco-Matic.csproj
  ```

All data and output files are handled automatically. The sample inventory (`data/inventory.csv`) is preserved and parsed automatically.

## VI. Functional Requirements

### Customer Functions:
- **Insert Money**: Add balance using visual inputs.
- **Select Item**: Click physical representation of items to purchase.
- **Examine Item**: See detailed visual flavor text and specific properties (like Calories or Volume) depending on if it's a Snack or Drink.
- **Recycle for Credit**: Click to recycle specific materials to instantly boost the simulated balance.
- **Get Change**: Print a visual receipt and return to the main hub.

### Administrator Functions:
- **Password-protected Login**: Secure gateway to the management backend.
- **Restock Items**: Quickly refill stock capacities dynamically updating the UI limits.
- **Add Item**: Define completely new items, selecting subclasses (Snack, Drink, Misc) which dictates what data fields are required.
- **Remove Item**: Delete a product permanently from the data model and visual grid.
- **Event Logging**: View all systemic transactions loaded from the background log.

### System Functions:
- **Data Persistence**: Automatically load, pattern match, and save the polymorphic inventory from/to a CSV file.
- **Event Logging**: Automatically log all purchases, recycling activities, and admin actions with localized timestamps mapping cleanly to domain `ProductType` enums.
- **Architecture**: Complete separation between Models, Data logic, and XAML/Code-Behind UI boundaries. 

## VII. OOP Concepts Demonstrated (Major Upgrade from Console)

- **Abstract Classes**: Replaced single monolithic structs with an abstract `VendingItem` core.
- **Inheritance**: `VendingItem` → `SnackItem`, `DrinkItem`, `MiscItem`. Each holds unique footprints (e.g., Drinks carry `VolumeML`).
- **Polymorphism**: The `DataStore` manages a master `List<VendingItem>`, casting up/down during UI mapping or CSV serialization based on type (`item is IHasCalories`).
- **Interfaces**: Established `IHasCalories` and `IHasVolume` strictly defining property contracts on the items.
- **Enums**: Utilized `ProductType` for strict typing over raw strings.

## VIII. Sustainable Development Goal (SDG) Connection

This project promotes **SDG 12: Responsible Consumption and Production** by:
- Integrating a "trash to credit" recycling system.
- Encouraging sustainable habits through gamification.
- Raising awareness about proper waste management.
- Creating a practical interactive representation of incentivized trash collection.

## IX. Author Notes

This serves as a major evolution of the original Eco-Matic Console program. By migrating to WPF, we replaced massive switch statements and monolithic code files with a properly factored Object-Oriented Architecture, making the system incredibly extensible and presentation-ready.

## License

This project is licensed under the **MIT License** - see the [LICENSE.txt](LICENSE.txt) file for details.