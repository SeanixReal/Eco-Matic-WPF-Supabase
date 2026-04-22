-- @block Create Database
CREATE DATABASE IF NOT EXISTS ecomatic_db;
USE ecomatic_db;

-- @block Roles
CREATE TABLE IF NOT EXISTS roles (
    role_id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255)
);

-- @block Vending Machines
CREATE TABLE IF NOT EXISTS vending_machines (
    machine_id INT AUTO_INCREMENT PRIMARY KEY,
    location_name VARCHAR(100) NOT NULL,
    status ENUM('Active', 'Maintenance', 'Offline') DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- @block Users
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

-- @block Global Item Catalog
-- Shared item identity lives here. Machines reuse these items by assignment.
CREATE TABLE IF NOT EXISTS items (
    item_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(50) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    calories INT DEFAULT 0,
    image_path VARCHAR(255),
    dispense_message VARCHAR(255) DEFAULT 'Enjoy your item!',
    examine_message TEXT
);

-- @block Machine Inventory
-- Each machine may assign any subset of global items into 12 canonical slots.
-- slot_price is optional: NULL means inherit the global default price from items.price.
CREATE TABLE IF NOT EXISTS machine_inventory (
    inventory_id INT AUTO_INCREMENT PRIMARY KEY,
    machine_id INT NOT NULL,
    item_id INT NOT NULL,
    slot_id VARCHAR(10) NOT NULL,
    stock_level INT DEFAULT 0,
    max_capacity INT DEFAULT 15,
    slot_price DECIMAL(10, 2) NULL,
    FOREIGN KEY (machine_id) REFERENCES vending_machines(machine_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE,
    UNIQUE KEY unique_machine_slot (machine_id, slot_id)
);

-- Important app invariant:
-- The application treats slot_id as the string form of 1..12.
-- Example valid values: '1', '2', ..., '12'

-- @block Sales
CREATE TABLE IF NOT EXISTS sales_transactions (
    transaction_id INT AUTO_INCREMENT PRIMARY KEY,
    machine_id INT NOT NULL,
    item_id INT NOT NULL,
    amount_paid DECIMAL(10, 2) NOT NULL,
    transaction_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (machine_id) REFERENCES vending_machines(machine_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE
);

-- @block Event Logs
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

-- @block RFID Customers
CREATE TABLE IF NOT EXISTS customers (
    rfid_tag VARCHAR(50) PRIMARY KEY,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    eco_credits INT DEFAULT 0,
    registered_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- @block Seed Roles and Default Users
INSERT IGNORE INTO roles (role_id, role_name, description) VALUES
(1, 'Admin', 'Full access to all system features including sales and users.'),
(2, 'Inventory Manager', 'Access restricted to viewing and managing inventory stock for an assigned machine.');

INSERT IGNORE INTO users (user_id, username, password_hash, role_id, assigned_machine_id) VALUES
(1, 'admin', 'admin123', 1, NULL);

INSERT IGNORE INTO users (user_id, username, password_hash, role_id, assigned_machine_id) VALUES
(2, 'inv_manager', 'manager123', 2, NULL);
