# Eco-Matic Architecture Documentation

## Overview
Eco-Matic is a modern, eco-friendly vending machine application built with .NET WPF. It features a dual-interface system for customers (vending) and administrators (inventory management).

## System Components

### 1. Presentation Layer (WPF)
- **CustomerWindow**: A high-fidelity, side-by-side layout mimicking a physical vending machine. It uses a `UniformGrid` for products and a dedicated control panel for payments and recycling.
- **AdminWindow**: Provides dashboard views for inventory levels, machine status, and transaction logs.
- **Modern Styling**: Uses `LinearGradientBrush`, `DropShadowEffect`, and custom `ControlTemplates` to achieve a premium, glassy aesthetic.

### 2. Logic & Utilities
- **ImageLoader**: A robust utility that handles the loading of product images from both embedded resources (Pack URIs) and local file paths, ensuring UI reliability even if assets are moved.
- **Refresh Logic**: The UI dynamically updates based on real-time stock levels, using visual triggers for "Out of Stock" states.

### 3. Data Layer
- **MySqlStore**: Encapsulates all database operations using MySQL. It handles:
  - Product inventory (stock, price, images).
  - Transaction logging.
  - User authentication (Admin/Customer).
- **Models**: Simple POCO classes (e.g., `Product`, `User`) represent data entities.

## Design Decisions
- **Side-by-Side Layout**: Chosen over vertical stacking to maximize screen real estate and mimic real-world machine ergonomics.
- **Resource Embedding**: Assets are set as `Resource` in the `.csproj` to bundle them into the assembly for easier deployment.
- **Dynamic Grid**: The product grid supports 12 items (3x4) with adaptive scaling to prevent image clipping.
