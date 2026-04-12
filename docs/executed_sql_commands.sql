-- 1. Create the main database
CREATE DATABASE IF NOT EXISTS ecomatic_db;

-- 2. Use the created database for subsequent commands
USE ecomatic_db;

-- 3. Create the roles table to define Admin and Inventory Manager roles
CREATE TABLE IF NOT EXISTS roles (
    role_id INT AUTO_INCREMENT PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255)
);

-- 4. Create the vending machines table to store active machine instances
CREATE TABLE IF NOT EXISTS vending_machines (
    machine_id INT AUTO_INCREMENT PRIMARY KEY,
    location_name VARCHAR(100) NOT NULL,
    status ENUM('Active', 'Maintenance', 'Offline') DEFAULT 'Active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 5. Create the users table to store staff accounts with their roles and assigned machines
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

-- 6. Create the items table as a global catalog of all available products
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

-- 7. Create the machine_inventory table to track stock levels per machine
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

-- 8. Create the sales_transactions table to record all customer purchases
CREATE TABLE IF NOT EXISTS sales_transactions (
    transaction_id INT AUTO_INCREMENT PRIMARY KEY,
    machine_id INT NOT NULL,
    item_id INT NOT NULL,
    amount_paid DECIMAL(10, 2) NOT NULL,
    transaction_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (machine_id) REFERENCES vending_machines(machine_id) ON DELETE CASCADE,
    FOREIGN KEY (item_id) REFERENCES items(item_id) ON DELETE CASCADE
);

-- 9. Create the event_logs table to track system events and actions
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

-- 10. Insert default Admin and Inventory Manager roles
INSERT IGNORE INTO roles (role_id, role_name, description) VALUES
(1, 'Admin', 'Full access to all system features including sales and users.'),
(2, 'Inventory Manager', 'Access restricted to viewing and managing inventory stock for an assigned machine.');

-- 11. Insert default Admin user account
INSERT IGNORE INTO users (user_id, username, password_hash, role_id, assigned_machine_id) VALUES
(1, 'admin', 'admin123', 1, NULL);

-- 12. Insert default Inventory Manager user account
INSERT IGNORE INTO users (user_id, username, password_hash, role_id, assigned_machine_id) VALUES
(2, 'inv_manager', 'manager123', 2, NULL);
