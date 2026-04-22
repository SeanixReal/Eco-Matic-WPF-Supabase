namespace Eco_Matic.Data;

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

public sealed class OfflineStoreSettings
{
    public string Host { get; init; } = "localhost";
    public uint Port { get; init; } = 3306;
    public string Username { get; init; } = "root";
    public string Password { get; init; } = "admin123";
    public string Schema { get; init; } = "eco_matic_latest";

    public static OfflineStoreSettings Load()
    {
        uint port = 3306;
        string? portValue = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_PORT");
        if (!string.IsNullOrWhiteSpace(portValue) && uint.TryParse(portValue, out uint parsedPort))
        {
            port = parsedPort;
        }

        return new OfflineStoreSettings
        {
            Host = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_HOST") ?? "localhost",
            Port = port,
            Username = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_USER") ?? "root",
            Password = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_PASSWORD") ?? "admin123",
            Schema = Environment.GetEnvironmentVariable("ECOMATIC_LOCAL_MYSQL_SCHEMA") ?? "eco_matic_latest"
        };
    }
}
