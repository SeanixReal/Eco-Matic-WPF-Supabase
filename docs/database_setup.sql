-- @block Create Database
-- Creates the main database for the Eco-Matic vending machine system.
CREATE DATABASE IF NOT EXISTS ecomatic_db;
USE ecomatic_db;

-- @block Create Roles Table
-- Defines the RBAC (Role-Based Access Control) roles. We will have 'Admin' and 'Inventory Manager'.
CREATE TABLE IF NOT EXISTS roles (
    role_id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255)
);

-- @block Create Vending Machines Table
-- Represents the different physical Eco-Matic instances. 
-- Admin adds these manually in the Vending Machine Manager tab.
CREATE TABLE IF NOT EXISTS vending_machines (
    machine_id INT AUTO_INCREMENT PRIMARY KEY,
    location_name VARCHAR(100) NOT NULL,
    status ENUM('Active', 'Maintenance', 'Offline') DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- @block Create Users Table
-- Stores the staff accounts. 
-- 'assigned_machine_id' securely chains an Inventory Manager to ONLY their specific machine. Admins will have a NULL assigned machine to access all.
CREATE TABLE IF NOT EXISTS users (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role_id INT NOT NULL,
    assigned_machine_id INT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (role_id) REFERENCES roles(role_id),
    FOREIGN KEY (assigned_machine_id) REFERENCES vending_machines(machine_id) ON DELETE SET NULL
);

-- @block Create Global Items Catalog
-- Stores the master list of all products (name, price, image, calories) available to ANY vending machine.
CREATE TABLE IF NOT EXISTS items (
    item_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    calories INT DEFAULT 0,
    volume_ml INT DEFAULT 0,
    flavor_text TEXT,
    image_path VARCHAR(255)
);

-- @block Create Machine Inventory Table
-- Maps the stock levels of specific items to specific vending machines. (Machine 1 has its own stock, Machine 2 has its own, etc.)
CREATE TABLE IF NOT EXISTS machine_inventory (
    inventory_id INT AUTO_INCREMENT PRIMARY KEY,
    machine_id INT NOT NULL,
    item_id INT NOT NULL,
    stock_level INT DEFAULT 0,
    max_capacity INT DEFAULT 15,
    FOREIGN KEY (machine_id) REFERENCES vending_machines(machine_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE,
    UNIQUE KEY unique_machine_item (machine_id, item_id)
);

-- @block Create Sales/Transactions Table
-- Records every purchase made by customers. Used by the Admin to generate the Sales Report tab.
CREATE TABLE IF NOT EXISTS sales_transactions (
    transaction_id INT AUTO_INCREMENT PRIMARY KEY,
    machine_id INT NOT NULL,
    item_id INT NOT NULL,
    amount_paid DECIMAL(10, 2) NOT NULL,
    transaction_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (machine_id) REFERENCES vending_machines(machine_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE
);

-- @block Create Event Logs Table
-- Tracks system events like restocks, login attempts, and errors to display in the Event Log tab.
CREATE TABLE IF NOT EXISTS event_logs (
    log_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NULL,
    machine_id INT NULL,
    event_type VARCHAR(50) NOT NULL,
    description TEXT NOT NULL,
    log_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE SET NULL,
    FOREIGN KEY (machine_id) REFERENCES vending_machines(machine_id) ON DELETE SET NULL
);

-- @block Insert Initial Setup Data
-- Populates the default roles and a default admin account (password: admin123).
-- Note: NO vending machines are pre-seeded anymore. You must add them in the Admin Panel.
INSERT IGNORE INTO roles (role_id, role_name, description) VALUES
(1, 'Admin', 'Full access to all system features including sales and users.'),
(2, 'Inventory Manager', 'Access restricted to viewing and managing inventory stock for an assigned machine.');

INSERT IGNORE INTO users (user_id, username, password_hash, role_id, assigned_machine_id) VALUES
(1, 'admin', 'admin123', 1, NULL);

-- @block Insert Default Inventory Manager Account
-- Added default Inventory Manager account for testing (password: manager123).
INSERT IGNORE INTO users (user_id, username, password_hash, role_id, assigned_machine_id) VALUES
(2, 'inv_manager', 'manager123', 2, NULL);
