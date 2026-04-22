using System.Data;
using MySqlConnector;

namespace Eco_Matic.Data;

public sealed class OfflineMySqlStore
{
    private const int CurrentSchemaVersion = 1;
    private readonly OfflineStoreSettings _settings = OfflineStoreSettings.Load();

    public void EnsureCreated()
    {
        ValidateSchemaName(_settings.Schema);

        using var serverConnection = OpenServerConnection();
        serverConnection.Open();

        using (var createSchema = serverConnection.CreateCommand())
        {
            createSchema.CommandText = $"CREATE DATABASE IF NOT EXISTS `{_settings.Schema}`;";
            createSchema.ExecuteNonQuery();
        }

        using var schemaConnection = OpenSchemaConnection();
        schemaConnection.Open();

        using var transaction = schemaConnection.BeginTransaction();
        ExecuteNonQuery(schemaConnection, transaction, """
            CREATE TABLE IF NOT EXISTS cached_vending_machines (
                machine_id INT NOT NULL PRIMARY KEY,
                location_name VARCHAR(100) NOT NULL,
                status VARCHAR(50) NOT NULL,
                last_synced_utc DATETIME NOT NULL
            );
            """);

        ExecuteNonQuery(schemaConnection, transaction, """
            CREATE TABLE IF NOT EXISTS cached_machine_inventory (
                inventory_id INT NOT NULL PRIMARY KEY,
                machine_id INT NOT NULL,
                slot_id VARCHAR(10) NOT NULL,
                slot_sort INT NOT NULL,
                item_id INT NOT NULL,
                item_name VARCHAR(100) NOT NULL,
                item_type VARCHAR(50) NOT NULL,
                default_price DECIMAL(10, 2) NOT NULL,
                slot_price DECIMAL(10, 2) NULL,
                effective_price DECIMAL(10, 2) NOT NULL,
                calories INT NOT NULL DEFAULT 0,
                image_path VARCHAR(255) NULL,
                dispense_message VARCHAR(255) NULL,
                examine_message TEXT NULL,
                stock_level INT NOT NULL DEFAULT 0,
                max_capacity INT NOT NULL DEFAULT 15,
                dirty_stock TINYINT(1) NOT NULL DEFAULT 0,
                dirty_updated_utc DATETIME NULL,
                last_synced_utc DATETIME NOT NULL,
                INDEX idx_cached_inventory_machine_slot (machine_id, slot_sort)
            );
            """);

        ExecuteNonQuery(schemaConnection, transaction, """
            CREATE TABLE IF NOT EXISTS sync_queue (
                queue_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                queue_type VARCHAR(32) NOT NULL,
                client_sync_id CHAR(36) NOT NULL,
                machine_id INT NOT NULL,
                inventory_id INT NULL,
                item_id INT NULL,
                amount_paid DECIMAL(10, 2) NULL,
                event_type VARCHAR(50) NULL,
                description TEXT NULL,
                occurred_utc DATETIME NOT NULL,
                payload_json LONGTEXT NULL,
                sync_status VARCHAR(16) NOT NULL DEFAULT 'Pending',
                synced_utc DATETIME NULL,
                UNIQUE KEY uq_sync_queue_client_sync_id (client_sync_id),
                INDEX idx_sync_queue_status_occurred (sync_status, occurred_utc, queue_id)
            );
            """);

        ExecuteNonQuery(schemaConnection, transaction, """
            CREATE TABLE IF NOT EXISTS sync_metadata (
                metadata_key VARCHAR(64) NOT NULL PRIMARY KEY,
                metadata_value TEXT NULL,
                updated_utc DATETIME NOT NULL
            );
            """);

        SetMetadata(schemaConnection, transaction, "schema_version", CurrentSchemaVersion.ToString(), DateTime.UtcNow);
        transaction.Commit();
    }

    public OfflineSyncMetadata GetMetadata()
    {
        using var connection = OpenSchemaConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT metadata_key, metadata_value FROM sync_metadata;";

        using var reader = command.ExecuteReader();
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            values[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
        }

        bool hasCompletedInitialSync = values.TryGetValue("has_completed_initial_sync", out string? hasSyncValue) &&
            bool.TryParse(hasSyncValue, out bool parsedHasSync) &&
            parsedHasSync;

        DateTime? lastSuccessfulSyncUtc = null;
        if (values.TryGetValue("last_successful_sync_utc", out string? lastSyncValue) &&
            DateTime.TryParse(lastSyncValue, out DateTime parsedSyncUtc))
        {
            lastSuccessfulSyncUtc = DateTime.SpecifyKind(parsedSyncUtc, DateTimeKind.Utc);
        }

        int schemaVersion = CurrentSchemaVersion;
        if (values.TryGetValue("schema_version", out string? schemaVersionValue) &&
            int.TryParse(schemaVersionValue, out int parsedSchemaVersion))
        {
            schemaVersion = parsedSchemaVersion;
        }

        return new OfflineSyncMetadata
        {
            HasCompletedInitialSync = hasCompletedInitialSync,
            LastSuccessfulSyncUtc = lastSuccessfulSyncUtc,
            SchemaVersion = schemaVersion
        };
    }

    public void MarkSuccessfulSync(DateTime syncedUtc)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        SetMetadata(connection, transaction, "has_completed_initial_sync", bool.TrueString, syncedUtc);
        SetMetadata(connection, transaction, "last_successful_sync_utc", syncedUtc.ToString("O"), syncedUtc);
        SetMetadata(connection, transaction, "schema_version", CurrentSchemaVersion.ToString(), syncedUtc);
        transaction.Commit();
    }

    public DataTable GetCachedVendingMachinesLookup()
    {
        var dt = new DataTable();
        dt.Columns.Add("machine_id", typeof(int));
        dt.Columns.Add("location_name", typeof(string));
        dt.Columns.Add("status", typeof(string));

        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT machine_id, location_name, status
            FROM cached_vending_machines
            ORDER BY machine_id;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            dt.Rows.Add(
                reader.GetInt32("machine_id"),
                reader.GetString("location_name"),
                reader.GetString("status"));
        }

        return dt;
    }

    public DataTable GetCachedMachineInventory(int machineId)
    {
        var dt = new DataTable();
        dt.Columns.Add("Slot", typeof(string));
        dt.Columns.Add("_SlotSort", typeof(int));
        dt.Columns.Add("_InventoryID", typeof(int));
        dt.Columns.Add("_ItemID", typeof(int));
        dt.Columns.Add("Image", typeof(string));
        dt.Columns.Add("Item", typeof(string));
        dt.Columns.Add("Type", typeof(string));
        dt.Columns.Add("Default Price", typeof(decimal));
        dt.Columns.Add("Slot Price", typeof(decimal));
        dt.Columns.Add("Price", typeof(decimal));
        dt.Columns.Add("Calories", typeof(int));
        dt.Columns.Add("Dispense Message", typeof(string));
        dt.Columns.Add("Examine Message", typeof(string));
        dt.Columns.Add("Stock", typeof(int));
        dt.Columns.Add("Max Capacity", typeof(int));

        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT inventory_id, item_id, slot_id, slot_sort, image_path, item_name, item_type,
                   default_price, slot_price, effective_price, calories, dispense_message,
                   examine_message, stock_level, max_capacity
            FROM cached_machine_inventory
            WHERE machine_id = @machine_id
            ORDER BY slot_sort, inventory_id;
            """;
        command.Parameters.AddWithValue("@machine_id", machineId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            object slotPrice = reader.IsDBNull("slot_price")
                ? DBNull.Value
                : reader.GetDecimal("slot_price");

            dt.Rows.Add(
                reader.GetString("slot_id"),
                reader.GetInt32("slot_sort"),
                reader.GetInt32("inventory_id"),
                reader.GetInt32("item_id"),
                reader.GetString("image_path"),
                reader.GetString("item_name"),
                reader.GetString("item_type"),
                reader.GetDecimal("default_price"),
                slotPrice,
                reader.GetDecimal("effective_price"),
                reader.GetInt32("calories"),
                reader.GetString("dispense_message"),
                reader.GetString("examine_message"),
                reader.GetInt32("stock_level"),
                reader.GetInt32("max_capacity"));
        }

        return dt;
    }

    public void ReplaceCache(DataTable machines, IReadOnlyDictionary<int, DataTable> inventoryByMachine, DateTime syncedUtc)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        ExecuteNonQuery(connection, transaction, "DELETE FROM cached_machine_inventory;");
        ExecuteNonQuery(connection, transaction, "DELETE FROM cached_vending_machines;");

        foreach (DataRow machineRow in machines.Rows)
        {
            using var machineCommand = connection.CreateCommand();
            machineCommand.Transaction = transaction;
            machineCommand.CommandText = """
                INSERT INTO cached_vending_machines (machine_id, location_name, status, last_synced_utc)
                VALUES (@machine_id, @location_name, @status, @last_synced_utc);
                """;
            machineCommand.Parameters.AddWithValue("@machine_id", Convert.ToInt32(machineRow["machine_id"]));
            machineCommand.Parameters.AddWithValue("@location_name", machineRow["location_name"]?.ToString() ?? "Unknown");
            machineCommand.Parameters.AddWithValue("@status", machineRow["status"]?.ToString() ?? "Unknown");
            machineCommand.Parameters.AddWithValue("@last_synced_utc", syncedUtc);
            machineCommand.ExecuteNonQuery();
        }

        foreach ((int machineId, DataTable inventory) in inventoryByMachine)
        {
            foreach (DataRow row in inventory.Rows)
            {
                using var inventoryCommand = connection.CreateCommand();
                inventoryCommand.Transaction = transaction;
                inventoryCommand.CommandText = """
                    INSERT INTO cached_machine_inventory (
                        inventory_id, machine_id, slot_id, slot_sort, item_id, item_name, item_type,
                        default_price, slot_price, effective_price, calories, image_path, dispense_message,
                        examine_message, stock_level, max_capacity, dirty_stock, dirty_updated_utc, last_synced_utc)
                    VALUES (
                        @inventory_id, @machine_id, @slot_id, @slot_sort, @item_id, @item_name, @item_type,
                        @default_price, @slot_price, @effective_price, @calories, @image_path, @dispense_message,
                        @examine_message, @stock_level, @max_capacity, 0, NULL, @last_synced_utc);
                    """;

                inventoryCommand.Parameters.AddWithValue("@inventory_id", Convert.ToInt32(row["_InventoryID"]));
                inventoryCommand.Parameters.AddWithValue("@machine_id", machineId);
                inventoryCommand.Parameters.AddWithValue("@slot_id", row["Slot"]?.ToString() ?? "");
                inventoryCommand.Parameters.AddWithValue("@slot_sort", Convert.ToInt32(row["_SlotSort"]));
                inventoryCommand.Parameters.AddWithValue("@item_id", Convert.ToInt32(row["_ItemID"]));
                inventoryCommand.Parameters.AddWithValue("@item_name", row["Item"]?.ToString() ?? "Unknown");
                inventoryCommand.Parameters.AddWithValue("@item_type", row["Type"]?.ToString() ?? "Misc");
                inventoryCommand.Parameters.AddWithValue("@default_price", Convert.ToDecimal(row["Default Price"]));
                object slotPrice = row["Slot Price"] == DBNull.Value ? DBNull.Value : Convert.ToDecimal(row["Slot Price"]);
                inventoryCommand.Parameters.AddWithValue("@slot_price", slotPrice);
                inventoryCommand.Parameters.AddWithValue("@effective_price", Convert.ToDecimal(row["Price"]));
                inventoryCommand.Parameters.AddWithValue("@calories", row["Calories"] == DBNull.Value ? 0 : Convert.ToInt32(row["Calories"]));
                inventoryCommand.Parameters.AddWithValue("@image_path", row["Image"]?.ToString() ?? "");
                inventoryCommand.Parameters.AddWithValue("@dispense_message", row["Dispense Message"]?.ToString() ?? "Enjoy your item!");
                inventoryCommand.Parameters.AddWithValue("@examine_message", row["Examine Message"]?.ToString() ?? "A standard vending item.");
                inventoryCommand.Parameters.AddWithValue("@stock_level", Convert.ToInt32(row["Stock"]));
                inventoryCommand.Parameters.AddWithValue("@max_capacity", Convert.ToInt32(row["Max Capacity"]));
                inventoryCommand.Parameters.AddWithValue("@last_synced_utc", syncedUtc);
                inventoryCommand.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    public void SaveInventorySnapshot(int machineId, IEnumerable<Product> products, DateTime changedUtc)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        foreach (var product in products)
        {
            if (product.DbInventoryId <= 0)
            {
                continue;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE cached_machine_inventory
                SET stock_level = @stock_level,
                    dirty_stock = CASE WHEN stock_level <> @stock_level THEN 1 ELSE dirty_stock END,
                    dirty_updated_utc = CASE WHEN stock_level <> @stock_level THEN @dirty_updated_utc ELSE dirty_updated_utc END
                WHERE inventory_id = @inventory_id AND machine_id = @machine_id;
                """;
            command.Parameters.AddWithValue("@stock_level", product.Stock);
            command.Parameters.AddWithValue("@dirty_updated_utc", changedUtc);
            command.Parameters.AddWithValue("@inventory_id", product.DbInventoryId);
            command.Parameters.AddWithValue("@machine_id", machineId);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void EnqueueEventLog(string clientSyncId, int machineId, string eventType, string description, DateTime occurredUtc, string? payloadJson = null)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sync_queue (
                queue_type, client_sync_id, machine_id, inventory_id, item_id, amount_paid,
                event_type, description, occurred_utc, payload_json, sync_status, synced_utc)
            VALUES (
                'event_log', @client_sync_id, @machine_id, NULL, NULL, NULL,
                @event_type, @description, @occurred_utc, @payload_json, 'Pending', NULL);
            """;
        command.Parameters.AddWithValue("@client_sync_id", clientSyncId);
        command.Parameters.AddWithValue("@machine_id", machineId);
        command.Parameters.AddWithValue("@event_type", eventType);
        command.Parameters.AddWithValue("@description", description);
        command.Parameters.AddWithValue("@occurred_utc", occurredUtc);
        command.Parameters.AddWithValue("@payload_json", payloadJson ?? string.Empty);
        command.ExecuteNonQuery();
    }

    public void EnqueueSale(string clientSyncId, int machineId, int inventoryId, int itemId, decimal amountPaid, DateTime occurredUtc, string? payloadJson = null)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sync_queue (
                queue_type, client_sync_id, machine_id, inventory_id, item_id, amount_paid,
                event_type, description, occurred_utc, payload_json, sync_status, synced_utc)
            VALUES (
                'sale', @client_sync_id, @machine_id, @inventory_id, @item_id, @amount_paid,
                NULL, NULL, @occurred_utc, @payload_json, 'Pending', NULL);
            """;
        command.Parameters.AddWithValue("@client_sync_id", clientSyncId);
        command.Parameters.AddWithValue("@machine_id", machineId);
        command.Parameters.AddWithValue("@inventory_id", inventoryId);
        command.Parameters.AddWithValue("@item_id", itemId);
        command.Parameters.AddWithValue("@amount_paid", amountPaid);
        command.Parameters.AddWithValue("@occurred_utc", occurredUtc);
        command.Parameters.AddWithValue("@payload_json", payloadJson ?? string.Empty);
        command.ExecuteNonQuery();
    }

    public List<PendingSyncQueueItem> GetPendingQueue()
    {
        var items = new List<PendingSyncQueueItem>();

        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT queue_id, queue_type, client_sync_id, machine_id, inventory_id, item_id,
                   amount_paid, event_type, description, occurred_utc, payload_json
            FROM sync_queue
            WHERE sync_status = 'Pending'
            ORDER BY occurred_utc, queue_id;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new PendingSyncQueueItem
            {
                QueueId = reader.GetInt64("queue_id"),
                QueueType = reader.GetString("queue_type"),
                ClientSyncId = reader.GetString("client_sync_id"),
                MachineId = reader.GetInt32("machine_id"),
                InventoryId = reader.IsDBNull("inventory_id") ? null : reader.GetInt32("inventory_id"),
                ItemId = reader.IsDBNull("item_id") ? null : reader.GetInt32("item_id"),
                AmountPaid = reader.IsDBNull("amount_paid") ? null : reader.GetDecimal("amount_paid"),
                EventType = reader.IsDBNull("event_type") ? null : reader.GetString("event_type"),
                Description = reader.IsDBNull("description") ? null : reader.GetString("description"),
                OccurredUtc = DateTime.SpecifyKind(reader.GetDateTime("occurred_utc"), DateTimeKind.Utc),
                PayloadJson = reader.IsDBNull("payload_json") ? null : reader.GetString("payload_json")
            });
        }

        return items;
    }

    public List<DirtyInventoryRecord> GetDirtyInventory()
    {
        var items = new List<DirtyInventoryRecord>();

        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT inventory_id, machine_id, stock_level
            FROM cached_machine_inventory
            WHERE dirty_stock = 1
            ORDER BY machine_id, inventory_id;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new DirtyInventoryRecord
            {
                InventoryId = reader.GetInt32("inventory_id"),
                MachineId = reader.GetInt32("machine_id"),
                StockLevel = reader.GetInt32("stock_level")
            });
        }

        return items;
    }

    public void MarkQueueSynced(long queueId, DateTime syncedUtc)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE sync_queue
            SET sync_status = 'Synced',
                synced_utc = @synced_utc
            WHERE queue_id = @queue_id;
            """;
        command.Parameters.AddWithValue("@synced_utc", syncedUtc);
        command.Parameters.AddWithValue("@queue_id", queueId);
        command.ExecuteNonQuery();
    }

    public int? GetItemIdForInventory(int inventoryId)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT item_id
            FROM cached_machine_inventory
            WHERE inventory_id = @inventory_id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@inventory_id", inventoryId);

        object? result = command.ExecuteScalar();
        return result == null ? null : Convert.ToInt32(result);
    }

    public bool HasCachedMachines()
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM cached_vending_machines;";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool HasCachedInventory(int machineId)
    {
        using var connection = OpenSchemaConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM cached_machine_inventory
            WHERE machine_id = @machine_id;
            """;
        command.Parameters.AddWithValue("@machine_id", machineId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private MySqlConnection OpenServerConnection()
    {
        var builder = CreateBaseConnectionStringBuilder();
        builder.Database = string.Empty;
        return new MySqlConnection(builder.ConnectionString);
    }

    private MySqlConnection OpenSchemaConnection()
    {
        var builder = CreateBaseConnectionStringBuilder();
        builder.Database = _settings.Schema;
        return new MySqlConnection(builder.ConnectionString);
    }

    private MySqlConnectionStringBuilder CreateBaseConnectionStringBuilder()
    {
        return new MySqlConnectionStringBuilder
        {
            Server = _settings.Host,
            Port = _settings.Port,
            UserID = _settings.Username,
            Password = _settings.Password,
            SslMode = MySqlSslMode.None,
            AllowUserVariables = true,
            ConnectionTimeout = 5,
            DefaultCommandTimeout = 10
        };
    }

    private static void ExecuteNonQuery(MySqlConnection connection, MySqlTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static void SetMetadata(MySqlConnection connection, MySqlTransaction transaction, string key, string? value, DateTime updatedUtc)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO sync_metadata (metadata_key, metadata_value, updated_utc)
            VALUES (@metadata_key, @metadata_value, @updated_utc)
            ON DUPLICATE KEY UPDATE
                metadata_value = VALUES(metadata_value),
                updated_utc = VALUES(updated_utc);
            """;
        command.Parameters.AddWithValue("@metadata_key", key);
        command.Parameters.AddWithValue("@metadata_value", value);
        command.Parameters.AddWithValue("@updated_utc", updatedUtc);
        command.ExecuteNonQuery();
    }

    private static void ValidateSchemaName(string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName) || !schemaName.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
        {
            throw new InvalidOperationException("Local offline MySQL schema name must contain only letters, numbers, or underscores.");
        }
    }
}
