namespace Eco_Matic.Data;

public enum SessionDataSource
{
    Unresolved = 0,
    Supabase = 1,
    LocalMySql = 2,
    Unavailable = 3
}

public sealed class OfflineSyncMetadata
{
    public bool HasCompletedInitialSync { get; init; }
    public DateTime? LastSuccessfulSyncUtc { get; init; }
    public int SchemaVersion { get; init; } = 1;
}

public sealed class PendingSyncQueueItem
{
    public long QueueId { get; init; }
    public string QueueType { get; init; } = string.Empty;
    public string ClientSyncId { get; init; } = string.Empty;
    public int MachineId { get; init; }
    public int? InventoryId { get; init; }
    public int? ItemId { get; init; }
    public decimal? AmountPaid { get; init; }
    public string? EventType { get; init; }
    public string? Description { get; init; }
    public DateTime OccurredUtc { get; init; }
    public string? PayloadJson { get; init; }
}

public sealed class DirtyInventoryRecord
{
    public int InventoryId { get; init; }
    public int MachineId { get; init; }
    public int StockLevel { get; init; }
}

public sealed class OfflineReceiptSessionRecord
{
    public string ClientSyncId { get; init; } = string.Empty;
    public string ReceiptNumber { get; init; } = string.Empty;
    public int MachineId { get; init; }
    public DateTime SessionStartedAt { get; init; }
    public DateTime SessionEndedAt { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal ChangeAmount { get; init; }
    public int RecyclePointsTotal { get; init; }
    public string Source { get; init; } = "online";
    public DateTime SavedUtc { get; init; }
}

public sealed class OfflineReceiptSessionLineRecord
{
    public string ClientSyncId { get; init; } = string.Empty;
    public int LineOrder { get; init; }
    public string EntryType { get; init; } = string.Empty;
    public string? SlotId { get; init; }
    public string? ItemName { get; init; }
    public int? Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? LineTotal { get; init; }
    public string? RecycleMaterial { get; init; }
    public int? RecyclePieces { get; init; }
    public int? RecyclePoints { get; init; }
}

public sealed class OfflineStoreSettings
{
    public string Host { get; init; } = string.Empty;
    public uint Port { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Schema { get; init; } = string.Empty;

    public static OfflineStoreSettings Load()
    {
        return TryLoad() ?? throw new AppConfigurationException(
            "Local offline MySQL is not fully configured. Either provide all ECOMATIC_LOCAL_MYSQL_* settings or remove them to run online-only.");
    }

    public static OfflineStoreSettings? TryLoad()
    {
        string? host = AppEnvironment.GetOptional("ECOMATIC_LOCAL_MYSQL_HOST");
        string? portValue = AppEnvironment.GetOptional("ECOMATIC_LOCAL_MYSQL_PORT");
        string? username = AppEnvironment.GetOptional("ECOMATIC_LOCAL_MYSQL_USER");
        string? password = AppEnvironment.GetOptional("ECOMATIC_LOCAL_MYSQL_PASSWORD");
        string? schema = AppEnvironment.GetOptional("ECOMATIC_LOCAL_MYSQL_SCHEMA");

        bool anyValueProvided =
            !string.IsNullOrWhiteSpace(host) ||
            !string.IsNullOrWhiteSpace(portValue) ||
            !string.IsNullOrWhiteSpace(username) ||
            !string.IsNullOrWhiteSpace(password) ||
            !string.IsNullOrWhiteSpace(schema);

        if (!anyValueProvided)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(portValue) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(schema))
        {
            throw new AppConfigurationException(
                "Local offline MySQL configuration is incomplete. Fill in all ECOMATIC_LOCAL_MYSQL_* settings or remove them all to run online-only.");
        }

        if (!uint.TryParse(portValue, out uint port) || port == 0)
        {
            throw new AppConfigurationException(
                "ECOMATIC_LOCAL_MYSQL_PORT must be a valid positive integer.");
        }

        return new OfflineStoreSettings
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            Schema = schema
        };
    }
}
