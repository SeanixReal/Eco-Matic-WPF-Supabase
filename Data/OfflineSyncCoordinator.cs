using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eco_Matic;

namespace Eco_Matic.Data;

public sealed class OfflineSyncCoordinator
{
    private readonly object _initializeLock = new();
    private readonly OfflineMySqlStore _localStore = new();
    private readonly SupabaseStore _cloudStore = new();
    private bool _initialized;
    public SessionDataSource CurrentSource { get; private set; } = SessionDataSource.Unresolved;
    public bool IsLocalStoreAvailable { get; private set; }
    public string? LocalStoreStatusMessage { get; private set; }

    private OfflineSyncCoordinator()
    {
    }

    public static OfflineSyncCoordinator Instance { get; } = new();

    public void InitializeApplication()
    {
        lock (_initializeLock)
        {
            if (_initialized)
            {
                return;
            }

            InitializeApplicationCore();
            _initialized = true;
        }
    }

    private void InitializeApplicationCore()
    {
        DataStore.HasCompletedInitialSync = false;
        DataStore.LastSuccessfulSyncUtc = null;

        if (_cloudStore.CanConnect())
        {
            CurrentSource = SessionDataSource.Supabase;
            IsLocalStoreAvailable = false;
            LocalStoreStatusMessage = "Supabase was selected as the data source for this session.";
            DataStore.IsOffline = false;
            return;
        }

        if (!_localStore.IsConfigured)
        {
            CurrentSource = SessionDataSource.Unavailable;
            IsLocalStoreAvailable = false;
            LocalStoreStatusMessage = "Supabase was unreachable at startup and local MySQL demo mode is not configured.";
            DataStore.IsOffline = true;
            return;
        }

        try
        {
            _localStore.EnsureCreated();
            CurrentSource = SessionDataSource.LocalMySql;
            IsLocalStoreAvailable = true;
            LocalStoreStatusMessage = "Local MySQL demo mode was selected for this session because Supabase was unreachable at startup.";
            DataStore.IsOffline = true;
        }
        catch (Exception ex)
        {
            CurrentSource = SessionDataSource.Unavailable;
            IsLocalStoreAvailable = false;
            LocalStoreStatusMessage = $"Supabase was unreachable at startup and local MySQL demo mode could not be prepared. {ex.Message}";
            DataStore.IsOffline = true;
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        InitializeApplication();
    }

    public void BeginBackgroundSync()
    {
        EnsureInitialized();

        DataStore.IsOffline = CurrentSource != SessionDataSource.Supabase;
    }

    public bool TrySyncIfOnline()
    {
        EnsureInitialized();

        bool isUsingSupabase = CurrentSource == SessionDataSource.Supabase;
        DataStore.IsOffline = !isUsingSupabase;
        return isUsingSupabase;
    }

    public bool CanEnterCustomerMode(out string message)
    {
        EnsureInitialized();

        if (CurrentSource == SessionDataSource.Supabase)
        {
            DataStore.IsOffline = false;
            DataTable liveMachines = _cloudStore.GetVendingMachinesLookup();
            if (liveMachines.Rows.Count == 0)
            {
                message = "No vending machines exist yet in Supabase. Add one in the Admin Console first.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        if (CurrentSource == SessionDataSource.LocalMySql && _localStore.HasCachedMachines())
        {
            DataStore.IsOffline = true;
            message = string.Empty;
            return true;
        }

        message = CurrentSource switch
        {
            SessionDataSource.LocalMySql => "This session started in local MySQL demo mode, but no vending machines are configured there.",
            SessionDataSource.Unavailable => "Supabase was unreachable at startup and local MySQL demo mode was not available.",
            _ => "No data source is available for customer mode."
        };
        return false;
    }

    public Task<(bool CanEnter, string Message)> PrepareCustomerModeAsync()
    {
        return Task.Run(() =>
        {
            EnsureInitialized();

            if (CurrentSource == SessionDataSource.Supabase)
            {
                DataStore.IsOffline = false;
                DataTable liveMachines = _cloudStore.GetVendingMachinesLookup();
                if (liveMachines.Rows.Count == 0)
                {
                    return (false, "No vending machines exist yet in Supabase. Add one in the Admin Console first.");
                }

                return (true, string.Empty);
            }

            if (CurrentSource == SessionDataSource.LocalMySql && _localStore.HasCachedMachines())
            {
                DataStore.IsOffline = true;
                return (true, string.Empty);
            }

            return CurrentSource switch
            {
                SessionDataSource.LocalMySql => (false, "This session started in local MySQL demo mode, but no vending machines are configured there."),
                SessionDataSource.Unavailable => (false, "Supabase was unreachable at startup and local MySQL demo mode was not available."),
                _ => (false, "No data source is available for customer mode.")
            };
        });
    }

    public bool CanUseOnlineOnlyFeature(out string message)
    {
        EnsureInitialized();

        if (CurrentSource == SessionDataSource.Supabase)
        {
            DataStore.IsOffline = false;
            message = string.Empty;
            return true;
        }

        message = CurrentSource == SessionDataSource.LocalMySql
            ? "This session started in local MySQL demo mode. Restart the app with internet access to use live Supabase-only features."
            : "This feature requires internet access and live Supabase connectivity.";
        return false;
    }

    public DataTable GetMachineLookupForCustomer(bool preferFreshWhenOnline = false)
    {
        EnsureInitialized();

        if (CurrentSource == SessionDataSource.Supabase)
        {
            DataStore.IsOffline = false;
            return _cloudStore.GetVendingMachinesLookup();
        }

        if (CurrentSource == SessionDataSource.LocalMySql)
        {
            DataStore.IsOffline = true;
            return _localStore.GetCachedVendingMachinesLookup();
        }

        DataStore.IsOffline = true;
        return CreateMachineLookupTable();
    }

    public DataTable GetMachineInventory(int machineId)
    {
        EnsureInitialized();

        if (CurrentSource == SessionDataSource.Supabase)
        {
            DataStore.IsOffline = false;
            return _cloudStore.GetMachineInventory(machineId);
        }

        if (CurrentSource == SessionDataSource.LocalMySql)
        {
            DataStore.IsOffline = true;
            return _localStore.GetCachedMachineInventory(machineId);
        }

        DataStore.IsOffline = true;
        return CreateInventoryTable();
    }

    public void SaveInventorySnapshot(int machineId, IEnumerable<Product> products)
    {
        EnsureInitialized();

        if (CurrentSource == SessionDataSource.Supabase)
        {
            foreach (Product product in products)
            {
                if (product.DbInventoryId > 0)
                {
                    _cloudStore.UpdateStock(product.DbInventoryId, product.Stock);
                }
            }

            DataStore.IsOffline = false;
            return;
        }

        if (CurrentSource == SessionDataSource.LocalMySql)
        {
            _localStore.SaveInventorySnapshot(machineId, products, DateTime.UtcNow);
        }

        DataStore.IsOffline = true;
    }

    public void SaveInventoryItem(int machineId, Product product)
    {
        EnsureInitialized();

        if (product.DbInventoryId <= 0)
        {
            return;
        }

        if (CurrentSource == SessionDataSource.Supabase)
        {
            _cloudStore.UpdateStock(product.DbInventoryId, product.Stock);
            DataStore.IsOffline = false;
            return;
        }

        if (CurrentSource == SessionDataSource.LocalMySql)
        {
            _localStore.SaveInventorySnapshot(machineId, new[] { product }, DateTime.UtcNow);
        }

        DataStore.IsOffline = true;
    }

    public void QueueEventLog(int machineId, string eventType, string description, decimal amount = 0m)
    {
        EnsureInitialized();

        string clientSyncId = Guid.NewGuid().ToString();

        if (CurrentSource == SessionDataSource.Supabase)
        {
            _cloudStore.InsertQueuedEventLog(clientSyncId, eventType, description, machineId, DateTime.UtcNow);
            DataStore.IsOffline = false;
            return;
        }

        if (CurrentSource == SessionDataSource.LocalMySql)
        {
            string payloadJson = JsonSerializer.Serialize(new
            {
                event_type = eventType,
                description,
                amount
            });

            _localStore.EnqueueEventLog(clientSyncId, machineId, eventType, description, DateTime.UtcNow, payloadJson);
        }

        DataStore.IsOffline = true;
    }

    public void QueueSale(int machineId, int inventoryId, decimal amountPaid)
    {
        EnsureInitialized();

        if (CurrentSource == SessionDataSource.Supabase)
        {
            _cloudStore.RecordSale(machineId, inventoryId, amountPaid);
            DataStore.IsOffline = false;
            return;
        }

        if (CurrentSource != SessionDataSource.LocalMySql)
        {
            DataStore.IsOffline = true;
            return;
        }

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
        DataStore.IsOffline = true;
    }

    public void QueueReceiptSession(Transaction transaction)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(transaction.ClientSyncId))
        {
            transaction.ClientSyncId = Guid.NewGuid().ToString();
        }

        if (CurrentSource == SessionDataSource.Supabase)
        {
            _cloudStore.InsertQueuedReceiptSession(transaction);
            DataStore.IsOffline = false;
            return;
        }

        if (CurrentSource == SessionDataSource.LocalMySql)
        {
            string payloadJson = JsonSerializer.Serialize(transaction);
            _localStore.SaveReceiptSession(transaction, payloadJson, DateTime.UtcNow);
        }

        DataStore.IsOffline = true;
    }

    private static DataTable CreateMachineLookupTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("machine_id", typeof(int));
        dt.Columns.Add("location_name", typeof(string));
        dt.Columns.Add("address_text", typeof(string));
        dt.Columns.Add("status", typeof(string));
        return dt;
    }

    private static DataTable CreateInventoryTable()
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
        return dt;
    }
}
