using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Eco_Matic.Data;

public partial class MySqlStore
{
    private readonly string _connectionString = "Server=127.0.0.1; Port=3306; Database=ecomatic_db; User ID=root; Password=admin123;";

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public (string? Role, int? AssignedMachineId) AuthenticateUser(string username, string password)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();

            string query = @"
                SELECT r.role_name, u.assigned_machine_id 
                FROM users u 
                JOIN roles r ON u.role_id = r.role_id 
                WHERE u.username = @user AND u.password_hash = @pass";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@pass", password);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string role = reader.GetString("role_name");
                int? machineId = reader.IsDBNull(reader.GetOrdinal("assigned_machine_id")) 
                    ? null 
                    : reader.GetInt32("assigned_machine_id");
                return (role, machineId);
            }
            return (null, null);
        }
        catch (MySqlException mex) when (mex.Number == 1049)
        {
            System.Windows.MessageBox.Show(
                "The database 'ecomatic_db' does not exist yet.\n\n" +
                "Please open 'docs/database_setup.sql' and run it.", 
                "Missing Database", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Warning);
            return (null, null);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Database connection failed: {ex.Message}");
            return (null, null);
        }
    }

    public System.Data.DataTable GetVendingMachines()
    {
        var dt = new System.Data.DataTable();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT machine_id as 'ID', location_name as 'Location', status as 'Status', created_at as 'Deployed' FROM vending_machines";
            using var cmd = new MySqlCommand(query, conn);
            using var adapter = new MySqlDataAdapter(cmd);
            adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load machines: {ex.Message}");
        }
        return dt;
    }

    public System.Data.DataTable GetVendingMachinesLookup()
    {
        var dt = new System.Data.DataTable();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            string query = "SELECT machine_id, location_name, status FROM vending_machines";
            using var cmd = new MySqlCommand(query, conn);
            using var adapter = new MySqlDataAdapter(cmd);
            adapter.Fill(dt);
        }
        catch {}
        return dt;
    }

    public bool AddMachine(string locationName)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();

            // Enforce max 4 vending machines
            using var countCmd = new MySqlCommand("SELECT COUNT(*) FROM vending_machines", conn);
            int currentCount = Convert.ToInt32(countCmd.ExecuteScalar());
            if (currentCount >= 4)
            {
                System.Windows.MessageBox.Show("Maximum of 4 vending machines allowed.", "Limit Reached", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return false;
            }

            using var cmd = new MySqlCommand("INSERT INTO vending_machines (location_name) VALUES (@loc)", conn);
            cmd.Parameters.AddWithValue("@loc", locationName);
            bool inserted = cmd.ExecuteNonQuery() > 0;
            
            if (inserted)
            {
                // After safely creating the new machine, let's grab its newly created machine_id
                using var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID()", conn);
                int newMachineId = Convert.ToInt32(idCmd.ExecuteScalar());

                // Auto-populate this brand new machine with EVERY available master item in the global items table
                // This initializes every item in the machine's inventory to 15 (max capacity).
                string popScript = @"
                    INSERT INTO machine_inventory (machine_id, item_id, stock_level)
                    SELECT @newId, item_id, 15
                    FROM items
                    WHERE NOT EXISTS (
                        SELECT 1 FROM machine_inventory 
                        WHERE machine_id = @newId AND item_id = items.item_id
                    )";
                using var popCmd = new MySqlCommand(popScript, conn);
                popCmd.Parameters.AddWithValue("@newId", newMachineId);
                popCmd.ExecuteNonQuery();
            }

            return inserted;
        }
        catch { return false; }
    }

    public bool DeleteMachine(int machineId)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("DELETE FROM vending_machines WHERE machine_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", machineId);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch { return false; }
    }

    public System.Data.DataTable GetRoles()
    {
        var dt = new System.Data.DataTable();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var adapter = new MySqlDataAdapter("SELECT role_id, role_name FROM roles", conn);
            adapter.Fill(dt);
        }
        catch {}
        return dt;
    }

    public System.Data.DataTable GetUsers()
    {
        var dt = new System.Data.DataTable();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            string query = @"SELECT u.user_id as 'ID', u.username as 'Username', r.role_name as 'Role', 
                             v.location_name as 'Assigned Machine' 
                             FROM users u
                             JOIN roles r ON u.role_id = r.role_id
                             LEFT JOIN vending_machines v ON u.assigned_machine_id = v.machine_id
                             WHERE r.role_name != 'Admin'";
            using var adapter = new MySqlDataAdapter(query, conn);
            adapter.Fill(dt);
        }
        catch {}
        return dt;
    }

    public bool AddUser(string username, string password, int roleId, int? assignedMachineId)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("INSERT INTO users (username, password_hash, role_id, assigned_machine_id) VALUES (@u, @p, @r, @m)", conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", password); // Password should be hashed in real app
            cmd.Parameters.AddWithValue("@r", roleId);
            cmd.Parameters.AddWithValue("@m", (object?)assignedMachineId ?? DBNull.Value);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch { return false; }
    }

    public bool DeleteUser(int userId)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("DELETE FROM users WHERE user_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch { return false; }
    }

    public System.Data.DataTable GetMachineInventory(int machineId)
    {
        var dt = new System.Data.DataTable();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            string query = @"
                SELECT 
                    mi.inventory_id AS 'ID',
                    i.image_path AS 'Image',
                    i.name AS 'Item',
                    i.type AS 'Type',
                    i.price AS 'Price',
                    i.calories AS 'Calories',
                    mi.stock_level AS 'Stock',
                    mi.max_capacity AS 'Max Capacity'
                FROM machine_inventory mi
                JOIN items i ON mi.item_id = i.item_id
                WHERE mi.machine_id = @machineId";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@machineId", machineId);
            using var adapter = new MySqlDataAdapter(cmd);
            adapter.Fill(dt);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to load inventory: {ex.Message}");
        }
        return dt;
    }
    public bool AddNewItemToMachine(int machineId, string name, string type, decimal price, int calories, int stock, int maxCap, string imagePath = "/Assets/Placeholder.png")
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            string q1 = "INSERT INTO items (name, type, price, calories, image_path) VALUES (@name, @type, @price, @calories, @img)";
            using var cmd1 = new MySqlCommand(q1, conn);
            cmd1.Parameters.AddWithValue("@name", name);
            cmd1.Parameters.AddWithValue("@type", type);
            cmd1.Parameters.AddWithValue("@price", price);
            cmd1.Parameters.AddWithValue("@calories", calories);
            cmd1.Parameters.AddWithValue("@img", imagePath);
            cmd1.ExecuteNonQuery();
            
            long itemId = cmd1.LastInsertedId;
            
            string q2 = "INSERT INTO machine_inventory (machine_id, item_id, stock_level, max_capacity) VALUES (@mid, @iid, @stock, @maxCap)";
            using var cmd2 = new MySqlCommand(q2, conn);
            cmd2.Parameters.AddWithValue("@mid", machineId);
            cmd2.Parameters.AddWithValue("@iid", itemId);
            cmd2.Parameters.AddWithValue("@stock", stock);
            cmd2.Parameters.AddWithValue("@maxCap", maxCap);
            cmd2.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to add item: {ex.Message}");
            return false;
        }
    }

    public bool RestockInventoryItem(int inventoryId, int quantity)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            string q1 = "SELECT max_capacity, stock_level FROM machine_inventory WHERE inventory_id = @id";
            using var cmd1 = new MySqlCommand(q1, conn);
            cmd1.Parameters.AddWithValue("@id", inventoryId);
            using var reader = cmd1.ExecuteReader();
            if (!reader.Read()) return false;
            int max = reader.GetInt32(0);
            int stock = reader.GetInt32(1);
            reader.Close();
            
            int total = stock + quantity;
            if (total > max)
            {
               System.Windows.MessageBox.Show($"Restock failed: Exceeds max capacity ({max}).", "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
               return false;
            }

            string q2 = "UPDATE machine_inventory SET stock_level = @total WHERE inventory_id = @id";
            using var cmd2 = new MySqlCommand(q2, conn);
            cmd2.Parameters.AddWithValue("@total", total);
            cmd2.Parameters.AddWithValue("@id", inventoryId);
            cmd2.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to restock item: {ex.Message}");
            return false;
        }
    }

    public void UpdateStock(int inventoryId, int newStock)
    {
        using var conn = GetConnection();
        conn.Open();
        string query = "UPDATE machine_inventory SET stock_level = @stock WHERE inventory_id = @id";
        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@stock", newStock);
        cmd.Parameters.AddWithValue("@id", inventoryId);
        cmd.ExecuteNonQuery();
    }

    public bool UpdateInventoryItem(int inventoryId, string name, string type, decimal price, int calories, string imagePath, int stock, int maxCap)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();

            string getSql = "SELECT item_id FROM machine_inventory WHERE inventory_id = @idx";
            using var cmdGet = new MySqlCommand(getSql, conn);
            cmdGet.Parameters.AddWithValue("@idx", inventoryId);
            object? itemIdObj = cmdGet.ExecuteScalar();
            if (itemIdObj == null) return false;
            int itemId = Convert.ToInt32(itemIdObj);

            string q1 = "UPDATE items SET name = @n, type = @t, price = @p, calories = @cal, image_path = @img WHERE item_id = @iid";
            using var cmd1 = new MySqlCommand(q1, conn);
            cmd1.Parameters.AddWithValue("@n", name);
            cmd1.Parameters.AddWithValue("@t", type);
            cmd1.Parameters.AddWithValue("@p", price);
            cmd1.Parameters.AddWithValue("@cal", calories);
            cmd1.Parameters.AddWithValue("@img", imagePath);
            cmd1.Parameters.AddWithValue("@iid", itemId);
            cmd1.ExecuteNonQuery();

            string q2 = "UPDATE machine_inventory SET stock_level = @s, max_capacity = @mc WHERE inventory_id = @invId";
            using var cmd2 = new MySqlCommand(q2, conn);
            cmd2.Parameters.AddWithValue("@s", stock);
            cmd2.Parameters.AddWithValue("@mc", maxCap);
            cmd2.Parameters.AddWithValue("@invId", inventoryId);
            cmd2.ExecuteNonQuery();

            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to update item: " + ex.Message);
            return false;
        }
    }

    public bool DeleteInventoryItem(int inventoryId)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            string getSql = "SELECT item_id FROM machine_inventory WHERE inventory_id = @idx";
            using var cmdGet = new MySqlCommand(getSql, conn);
            cmdGet.Parameters.AddWithValue("@idx", inventoryId);
            object? itemIdObj = cmdGet.ExecuteScalar();
            if (itemIdObj == null) return false;
            int itemId = Convert.ToInt32(itemIdObj);

            string q2 = "DELETE FROM machine_inventory WHERE inventory_id = @invId";
            using var cmd2 = new MySqlCommand(q2, conn);
            cmd2.Parameters.AddWithValue("@invId", inventoryId);
            cmd2.ExecuteNonQuery();

            string q1 = "DELETE FROM items WHERE item_id = @iid";
            using var cmd1 = new MySqlCommand(q1, conn);
            cmd1.Parameters.AddWithValue("@iid", itemId);
            cmd1.ExecuteNonQuery();

            return true;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Failed to delete item: " + ex.Message);
            return false;
        }
    }

    public System.Data.DataTable GetEventLogs()
    {
        var dt = new System.Data.DataTable();
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT log_id AS 'Log ID', log_date AS 'Timestamp', event_type AS 'Event', description AS 'Notes' FROM event_logs ORDER BY log_date DESC", conn);
            using var adapter = new MySqlDataAdapter(cmd);
            adapter.Fill(dt);
        }
        catch {}
        return dt;
    }

    public void RecordSale(int machineId, int inventoryId, decimal amountPaid)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            // Get item_id from machine_inventory
            using var fetchCmd = new MySqlCommand("SELECT item_id FROM machine_inventory WHERE inventory_id = @id", conn);
            fetchCmd.Parameters.AddWithValue("@id", inventoryId);
            var result = fetchCmd.ExecuteScalar();
            if (result == null) return;
            
            int itemId = Convert.ToInt32(result);

            using var salesCmd = new MySqlCommand("INSERT INTO sales_transactions (machine_id, item_id, amount_paid) VALUES (@m_id, @i_id, @a)", conn);
            salesCmd.Parameters.AddWithValue("@m_id", machineId);
            salesCmd.Parameters.AddWithValue("@i_id", itemId);
            salesCmd.Parameters.AddWithValue("@a", amountPaid);
            salesCmd.ExecuteNonQuery();
        }
        catch {}
    }

    public void LogEvent(string eventType, string details, decimal amount = 0m, int machineId = 1)
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("INSERT INTO event_logs (event_type, description, machine_id) VALUES (@e, @d, @m)", conn);
            cmd.Parameters.AddWithValue("@e", eventType);
            cmd.Parameters.AddWithValue("@d", details);
            cmd.Parameters.AddWithValue("@m", machineId);
            cmd.ExecuteNonQuery();
        }
        catch {}
    }

    public void ClearEventLogs()
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("DELETE FROM event_logs", conn);
            cmd.ExecuteNonQuery();
        }
        catch {}
    }

    public (decimal Daily, decimal Weekly, decimal Monthly, decimal Yearly) GetSalesTotals()
    {
        try
        {
            using var conn = GetConnection();
            conn.Open();

            decimal daily = 0, weekly = 0, monthly = 0, yearly = 0;

            string q = @"
                SELECT 
                    SUM(CASE WHEN DATE(transaction_date) = CURDATE() THEN amount_paid ELSE 0 END) as Daily,
                    SUM(CASE WHEN YEARWEEK(transaction_date, 1) = YEARWEEK(CURDATE(), 1) THEN amount_paid ELSE 0 END) as Weekly,
                    SUM(CASE WHEN YEAR(transaction_date) = YEAR(CURDATE()) AND MONTH(transaction_date) = MONTH(CURDATE()) THEN amount_paid ELSE 0 END) as Monthly,
                    SUM(CASE WHEN YEAR(transaction_date) = YEAR(CURDATE()) THEN amount_paid ELSE 0 END) as Yearly
                FROM sales_transactions";
            
            using var cmd = new MySqlCommand(q, conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                daily = reader["Daily"] != DBNull.Value ? Convert.ToDecimal(reader["Daily"]) : 0m;
                weekly = reader["Weekly"] != DBNull.Value ? Convert.ToDecimal(reader["Weekly"]) : 0m;
                monthly = reader["Monthly"] != DBNull.Value ? Convert.ToDecimal(reader["Monthly"]) : 0m;
                yearly = reader["Yearly"] != DBNull.Value ? Convert.ToDecimal(reader["Yearly"]) : 0m;
            }

            return (daily, weekly, monthly, yearly);
        }
        catch
        {
            return (0m, 0m, 0m, 0m);
        }
    }

    public (System.Data.DataTable Data, decimal Total) GetFilteredSales(DateTime date, string filterType)
    {
        var dt = new System.Data.DataTable();
        decimal total = 0m;
        try
        {
            using var conn = GetConnection();
            conn.Open();

            string condition;
            switch (filterType)
            {
                case "Week":
                    condition = "YEARWEEK(s.transaction_date, 1) = YEARWEEK(@date, 1)";
                    break;
                case "Month":
                    condition = "YEAR(s.transaction_date) = YEAR(@date) AND MONTH(s.transaction_date) = MONTH(@date)";
                    break;
                case "Year":
                    condition = "YEAR(s.transaction_date) = YEAR(@date)";
                    break;
                case "Day":
                default:
                    condition = "DATE(s.transaction_date) = DATE(@date)";
                    break;
            }

            string q = $@"SELECT 
                            s.transaction_id AS 'TX ID', 
                            s.transaction_date AS 'Date', 
                            m.location_name AS 'Machine', 
                            i.name AS 'Item', 
                            1 AS 'Quantity', 
                            i.price AS 'Price', 
                            s.amount_paid AS 'Total Paid',
                            CONCAT('Qty: 1 | Price: ₱', FORMAT(i.price, 2), ' | Total: ₱', FORMAT(s.amount_paid, 2)) AS 'Notes'
                         FROM sales_transactions s
                         JOIN vending_machines m ON s.machine_id = m.machine_id
                         JOIN items i ON s.item_id = i.item_id
                         WHERE {condition}
                         ORDER BY s.transaction_date DESC";
            
            using var cmd = new MySqlCommand(q, conn);
            cmd.Parameters.AddWithValue("@date", date);
            
            using var adapter = new MySqlDataAdapter(cmd);
            adapter.Fill(dt);

            string qTotal = $"SELECT SUM(amount_paid) FROM sales_transactions s WHERE {condition}";
            using var cmdTotal = new MySqlCommand(qTotal, conn);
            cmdTotal.Parameters.AddWithValue("@date", date);
            var resT = cmdTotal.ExecuteScalar();
            if (resT != DBNull.Value && resT != null) 
            {
                total = Convert.ToDecimal(resT);
            }
        }
        catch {}
        return (dt, total);
    }

    public void GetDashboardMetrics(out decimal totalSales, out int totalItemsSold, out int lowStockAlerts, out int activeMachines)
    {
        totalSales = 0m;
        totalItemsSold = 0;
        lowStockAlerts = 0;
        activeMachines = 0;

        try
        {
            using var conn = GetConnection();
            conn.Open();

            using var cmdTotal = new MySqlCommand("SELECT SUM(amount_paid) FROM sales_transactions", conn);
            var resT = cmdTotal.ExecuteScalar();
            if (resT != DBNull.Value && resT != null) totalSales = Convert.ToDecimal(resT);

            using var cmdItems = new MySqlCommand("SELECT COUNT(*) FROM sales_transactions", conn);
            var resI = cmdItems.ExecuteScalar();
            if (resI != DBNull.Value && resI != null) totalItemsSold = Convert.ToInt32(resI);

            using var cmdLowStock = new MySqlCommand("SELECT COUNT(*) FROM machine_inventory WHERE stock_level <= 3", conn);
            var resL = cmdLowStock.ExecuteScalar();
            if (resL != DBNull.Value && resL != null) lowStockAlerts = Convert.ToInt32(resL);

            using var cmdActive = new MySqlCommand("SELECT COUNT(*) FROM vending_machines WHERE status = 'Active'", conn);
            var resA = cmdActive.ExecuteScalar();
            if (resA != DBNull.Value && resA != null) activeMachines = Convert.ToInt32(resA);
        }
        catch {}
    }
}
