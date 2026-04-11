# Developer Presentation Guide: Eco-Matic WPF

This document is designed to help you confidently explain your codebase to a professor, peers, or interviewers. It covers how the application is structured, how Object-Oriented Programming (OOP) is fully utilized, and how it evolved from your original console application.

## 1. Core Architecture (How to explain the structure)

When explaining how the app works, split it into three main "layers":

1.  **The Models (`Models/VendingItem.cs`, `Models/Product.cs`)**
    *   *What to say:* "These are the blueprints for my data. Instead of just having simple data structs, I used full Object-Oriented Programming to define abstract base classes and specific subclasses for snacks, drinks, and miscellaneous items."
2.  **The Data Layer (`Data/DataStore.cs`, `Data/CsvStorage.cs`)**
    *   *What to say:* "This is the brain of the app. By using a centralized `DataStore` to hold the inventory, the UI windows don't have to worry about reading and writing files. The `CsvStorage` specifically handles serializing these complex objects into a flat CSV text file and reading them back out."
3.  **The UI Layer (`MainWindow.xaml.cs`, `AdminWindow.xaml.cs`, etc.)**
    *   *What to say:* "The user interface acts as a controller. When a user clicks a button like 'Add Item' or 'Buy', the UI just sends those commands directly to the underlying `DataStore` to process the business logic."

## 2. Explaining Object-Oriented Principles (OOP)

If you are asked, "How did you use OOP in this project?", here are your main talking points:

*   **Abstraction:** "I created an abstract base class called `VendingItem`. You cannot create a generic 'VendingItem' because it serves only as a template holding shared properties like `Name`, `Price`, and `ProductId`."
*   **Inheritance:** "I have concrete subclasses that *inherit* from `VendingItem`, specifically: `SnackItem`, `DrinkItem`, and `MiscItem`. This means they get all the generic item details, but they can have their own specialized data."
*   **Interfaces:** "To avoid tightly coupling properties, I utilized interfaces. For example, both `SnackItem` and `DrinkItem` implement `IHasCalories`, ensuring they possess a `Calories` property. `DrinkItem` exclusively implements `IHasVolume` for its `VolumeML` property."
*   **Polymorphism:** "The system is capable of treating all these differently structured items uniformly. In `DataStore`, they are all held securely inside a generic `List<VendingItem>`. When displaying them or saving them, the program dynamically checks their specific type (`item is IHasCalories`) to process their unique attributes correctly."

## 3. Explaining the Migration from Console to WPF

If asked, "What is the biggest difference between your previous console app and this?"

*   **Event-Driven vs. Linear:** "In the console app, the program strictly controlled the flow, prompting the user sequentially using `Console.ReadLine()`. The WPF app is event-driven; it simply waits for the user to trigger specific events, like an `OnClick` action."
*   **Data Binding & XAML:** "Instead of writing raw text to the screen continuously, WPF separates the interface design via XAML. It maps C# properties dynamically to Visual components."
*   **Strong Typing & Contracts:** "The console version heavily relied on `enums` and deep `switch` statements to figure out behavior. The new code enforces the behavior immediately within the Class definitions utilizing strict OOP overriding, making the code vastly cleaner and highly extensible."

## 4. Quick Q&A Cheat Sheet

*   **Q:** How does data save across reloads?
    **A:** The `CsvStorage` converts the object list into comma-separated text lines when changes occur. On Application Startup, it loops through those text lines, checks the type explicitly, and rebuilds the `DrinkItem` or `SnackItem` objects before pushing them into the UI.
*   **Q:** Why not just use one `Product` class with nullable fields?
    **A:** That breaks Single Responsibility and creates bloated classes. By using Inheritance, a `Misc` item doesn't waste memory carrying around null `Calories` and `VolumeML` variables that it will never use.