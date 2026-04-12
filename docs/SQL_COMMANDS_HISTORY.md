# SQL Commands History

This document tracks all manual SQL operations and terminal commands executed to provision and maintain the database.

## 1. Initial Database Setup
We constructed the schema and saved it directly to `docs/database_setup.sql`.
To apply the entire schema to your local MySQL instance, we executed the following native MySQL CLI command from the terminal:

```bash
mysql -u root -padmin123 < docs/database_setup.sql
```

## 2. Setting Up Accounts
The setup script previously only seeded the `admin` account. Based on your request, we have manually appended the `inv_manager` account directly into the local DB and into the SQL file.

**Command Executed:**
```bash
mysql -u root -padmin123 -e "USE ecomatic_db; INSERT IGNORE INTO users (user_id, username, password_hash, role_id, assigned_machine_id) VALUES (2, 'inv_manager', 'manager123', 2, NULL);"
```

This creates an Inventory Manager with the following credentials:
- **Username:** `inv_manager`
- **Password:** `manager123`
- **Role:** Inventory Manager

*Note: The `assigned_machine_id` is currently NULL. An admin will need to assign this user to a machine once a machine is created via the Admin panel.*

## 3. Important Notes
If you drop the database and need to start fresh, simply run the setup command again:
```bash
mysql -u root -padmin123 < docs/database_setup.sql
```
This will recreate everything from scratch using the latest definitions inside `database_setup.sql`, including the new `inv_manager` account.
