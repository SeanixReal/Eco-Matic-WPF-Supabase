# Eco-Matic: User Manual

Welcome to the Eco-Matic Vending Machine Administration Console. This guide will walk you through the core functionalities of the system.

## 1. Getting Started
*   **Login**: Enter your provided username and password. The system will determine whether you are a **Master Admin** or an **Inventory Manager**.
*   **Role Differences**:
    *   *Master Admin*: Full access to everything, including Sales, Logs, User creation, and all machine inventories.
    *   *Inventory Manager*: Restricted specifically to restocking items for the single vending machine assigned to them. All financial data is hidden.

## 2. Using the Dashboard
When logged in as a Master Admin, you'll immediately see the **System Overview**:
*   **Total Sales (₱)**: Cumulative revenue across all units.
*   **Items Sold**: The total volume of stock processed.
*   **Low Stock Alerts**: If an item in any machine drops below 5 units, this will turn red to alert you.
*   **Active Machines**: The number of vending machines currently online on your network (max 4).

## 3. Inventory Management
Select the **Inventory** tab on the left sidebar to manage machine stock.
*   **Machine Dropdown**: Select the vending machine location you wish to inspect. Returning the grid of up to 12 slots.
*   **Add New Item**: Clicking this allows you to create a brand new product, set its price, and specify its initial stock. Note that a vending machine cannot exceed 12 items.
*   **Restock Item**: Select an item in the list and hit Restock to instantaneously refill the `Stock` amount.
*   **Edit/Delete Item**: Modify item names, calorie values, prices, or completely remove them from the machine.

## 4. Viewing Sales & Reports
*   **Sales Filter**: Use the top-right filter to change the report's time range: Day, Week, Month, or Year. 
*   **Date Selector**: Pick a specific calendar date and the system will dynamically filter the database to reflect only transactions that occurred on that date (or within that week/month/year).

## 5. Event Logs & Activity Tracking
*   **Event Logs**: Every purchase, restock, edit, and deletion is recorded securely with a time-stamp.
*   **Clear Logs**: A big red button resets the log list. Use this sparingly if the event logs list becomes too cluttered for tracking.

## 6. Network Expansion (Vending Machines)
The **Vending Machines** tab allows you to configure your fleet.
*   **Add Machine**: Input a custom location name. The machine will automatically self-populate its empty slots ready for business.
*   **Edit Machine**: Change the location title or toggle the physical status (e.g., Active vs. Out of Order).

## 7. User Manager
*   Here, Master Admins can create new user credentials, set passwords, and assign them directly to manage a specific machine if they are an "Inventory Manager". Note: The Master Admin account cannot be deleted.