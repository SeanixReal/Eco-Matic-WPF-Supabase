using System.Data;
using System.Text.Json;

namespace Eco_Matic.Data;

public sealed class OfflineSyncCoordinator
{
    private readonly object _syncLock = new();
    private readonly OfflineMySqlStore _localStore = new();
    private readonly SupabaseStore _cloudStore = new();

    private OfflineSyncCoordinator()
    {
    }

    public static OfflineSyncCoordinator Instance { get; } = new();

    public void InitializeApplication()
    {
        _localStore.EnsureCreated();
        RefreshMetadataState();
        TrySyncIfOnline();
    }

    public bool TrySyncIfOnline()
    {
        lock (_syncLock)
        {
            if (!_cloudStore.CanConnect())
            {
                DataStore.IsOffline = true;
                RefreshMetadataState();
                return false;
            }

            try
            {
                ReplayPendingQueue();
                PushDirtyInventory();
                RefreshCacheFromCloud();
                DateTime syncedUtc = DateTime.UtcNow;
                _localStore.MarkSuccessfulSync(syncedUtc);
                DataStore.IsOffline = false;
                RefreshMetadataState();
                return true;
            }
            catch
            {
                DataStore.IsOffline = true;
                RefreshMetadataState();
                return false;
            }
        }
    }

    public bool CanEnterCustomerMode(out string message)
    {
        TrySyncIfOnline();

        if (!DataStore.HasCompletedInitialSync)
        {
            message = "Customer mode needs one successful online sync before it can work offline.";
            return false;
        }

        if (!_localStore.HasCachedMachines())
        {
            message = "No cached vending machine data is available yet. Run the app once with internet first.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    public bool CanUseOnlineOnlyFeature(out string message)
    {
        if (TrySyncIfOnline())
        {
            message = string.Empty;
            return true;
        }

        if (_cloudStore.CanConnect())
        {
            message = "Supabase is reachable, but offline replay could not finish. Apply docs/sql/migrations/supabase/migration_increment4.sql and retry.";
            return false;
        }

        message = "This feature requires internet access. Customer offline mode still works after a successful sync.";
        return false;
    }

    public DataTable GetMachineLookupForCustomer()
    {
        TrySyncIfOnline();
        return _localStore.GetCachedVendingMachinesLookup();
    }

    public DataTable GetMachineInventory(int machineId)
    {
        TrySyncIfOnline();

        if (!_localStore.HasCachedInventory(machineId) && _cloudStore.CanConnect())
        {
            RefreshCacheFromCloud();
        }

        return _localStore.GetCachedMachineInventory(machineId);
    }

    public void SaveInventorySnapshot(int machineId, IEnumerable<Product> products)
    {
        _localStore.SaveInventorySnapshot(machineId, products, DateTime.UtcNow);
        TrySyncIfOnline();
    }

    public void QueueEventLog(int machineId, string eventType, string description, decimal amount = 0m)
    {
        string clientSyncId = Guid.NewGuid().ToString();
        string payloadJson = JsonSerializer.Serialize(new
        {
            event_type = eventType,
            description,
            amount
        });

        _localStore.EnqueueEventLog(clientSyncId, machineId, eventType, description, DateTime.UtcNow, payloadJson);
        TrySyncIfOnline();
    }

    public void QueueSale(int machineId, int inventoryId, decimal amountPaid)
    {
        int? itemId = _localStore.GetItemIdForInventory(inventoryId);
        if (!itemId.HasValue)
        {
            throw new InvalidOperationException($"Could not resolve cached item_id for inventory {inventoryId}.");
        }

        string clientSyncId = Guid.NewGuid().ToString();
        string payloadJson = JsonSerializer.Serialize(new
        {
            machine_id = machineId,
            inventory_id = inventoryId,
            item_id = itemId.Value,
            amount_paid = amountPaid
        });

        _localStore.EnqueueSale(clientSyncId, machineId, inventoryId, itemId.Value, amountPaid, DateTime.UtcNow, payloadJson);
        TrySyncIfOnline();
    }

    private void ReplayPendingQueue()
    {
        foreach (PendingSyncQueueItem queueItem in _localStore.GetPendingQueue())
        {
            if (queueItem.QueueType == "event_log")
            {
                if (!_cloudStore.EventLogExists(queueItem.ClientSyncId))
                {
                    _cloudStore.InsertQueuedEventLog(
                        queueItem.ClientSyncId,
                        queueItem.EventType ?? "EVENT",
                        queueItem.Description ?? string.Empty,
                        queueItem.MachineId,
                        queueItem.OccurredUtc);
                }
            }
            else if (queueItem.QueueType == "sale")
            {
                if (!queueItem.ItemId.HasValue || !queueItem.AmountPaid.HasValue)
                {
                    throw new InvalidOperationException($"Pending sale queue item {queueItem.QueueId} is missing item or amount data.");
                }

                if (!_cloudStore.SaleExists(queueItem.ClientSyncId))
                {
                    _cloudStore.InsertQueuedSale(
                        queueItem.ClientSyncId,
                        queueItem.MachineId,
                        queueItem.ItemId.Value,
                        queueItem.AmountPaid.Value,
                        queueItem.OccurredUtc);
                }
            }

            _localStore.MarkQueueSynced(queueItem.QueueId, DateTime.UtcNow);
        }
    }

    private void PushDirtyInventory()
    {
        foreach (DirtyInventoryRecord record in _localStore.GetDirtyInventory())
        {
            _cloudStore.UpdateStock(record.InventoryId, record.StockLevel);
        }
    }

    private void RefreshCacheFromCloud()
    {
        DataTable machines = _cloudStore.GetVendingMachinesLookup();
        var inventoryByMachine = new Dictionary<int, DataTable>();

        foreach (DataRow machineRow in machines.Rows)
        {
            int machineId = Convert.ToInt32(machineRow["machine_id"]);
            inventoryByMachine[machineId] = _cloudStore.GetMachineInventory(machineId);
        }

        _localStore.ReplaceCache(machines, inventoryByMachine, DateTime.UtcNow);
    }

    private static void RefreshMetadataState()
    {
        OfflineSyncMetadata metadata = Instance._localStore.GetMetadata();
        DataStore.HasCompletedInitialSync = metadata.HasCompletedInitialSync;
        DataStore.LastSuccessfulSyncUtc = metadata.LastSuccessfulSyncUtc;
    }
}
