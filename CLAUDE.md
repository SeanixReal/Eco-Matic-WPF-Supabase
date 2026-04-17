# Eco-Matic Development Guide

## Build and Run Commands
- **Build**: `dotnet build`
- **Run**: `dotnet run --project Eco-Matic.csproj`
- **Clean**: `dotnet clean`

## Project Architecture
- **Framework**: .NET 10.0 WPF (Windows Presentation Foundation)
- **UI Design**: side-by-side vending machine layout with high-density product grid (3x4) and right-aligned control panel.
- **Data Storage**: MySQL (configured in `Data/MySqlStore.cs`)
- **Asset Management**: Embedded resources for images (Pack URIs) with physical file fallback.

## Coding Standards
- **XAML Styling**: Use centralized `Styles` in `Window.Resources`. 
- **Layout**: Prefer `Grid` and `Border` for structured, modern UI. Avoid hardcoded heights for scaling items.
- **Naming**: PascalCase for Classes and Methods; camelCase for local variables.
- **MVVM-lite**: Logic handled in code-behind (`.xaml.cs`) but decoupled via Service classes (e.g., `MySqlStore`).

## Key Files
- `CustomerWindow.xaml`: Main customer interface (Vending Machine).
- `AdminWindow.xaml`: Administrative and inventory management.
- `Utilities/ImageLoader.cs`: Core logic for loading product images.
- `Data/MySqlStore.cs`: Database interaction layer.
