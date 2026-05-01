using System.Data;
using Eco_Matic;

namespace Eco_Matic.Data;

public sealed class SupabaseSessionCoordinator
{
    private readonly object _initializeLock = new();
    private readonly SupabaseStore _cloudStore = new();
    private bool _initialized;

    private SupabaseSessionCoordinator()
    {
    }

    public static SupabaseSessionCoordinator Instance { get; } = new();

    public bool IsSupabaseAvailable { get; private set; }
    public string StatusMessage { get; private set; } = "Supabase availability has not been checked yet.";

    public void InitializeApplication()
    {
        lock (_initializeLock)
        {
            if (_initialized)
            {
                return;
            }

            RefreshAvailability();
            _initialized = true;
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

    private bool RefreshAvailability()
    {
        try
        {
            IsSupabaseAvailable = _cloudStore.CanConnect();
        }
        catch
        {
            IsSupabaseAvailable = false;
        }

        StatusMessage = IsSupabaseAvailable
            ? "Supabase is available for this session."
            : "Supabase is unreachable. Eco-Matic requires Supabase connectivity for customer and admin data.";

        return IsSupabaseAvailable;
    }

    public bool RefreshAvailabilityStatus()
    {
        EnsureInitialized();
        return RefreshAvailability();
    }

    public bool CanUseSupabaseFeature(out string message)
    {
        EnsureInitialized();

        if (!IsSupabaseAvailable)
        {
            RefreshAvailability();
        }

        message = IsSupabaseAvailable
            ? string.Empty
            : "This feature requires live Supabase connectivity. Please check the internet connection and try again.";

        return IsSupabaseAvailable;
    }

    public Task<(bool CanEnter, string Message)> PrepareCustomerModeAsync()
    {
        return Task.Run(() =>
        {
            EnsureInitialized();

            if (!IsSupabaseAvailable && !RefreshAvailability())
            {
                return (false, "Customer mode requires live Supabase connectivity. Please check the internet connection and try again.");
            }

            DataTable liveMachines = _cloudStore.GetVendingMachinesLookup();
            if (liveMachines.Rows.Count == 0)
            {
                return (false, "No vending machines exist yet in Supabase. Add one in the Admin Console first.");
            }

            return (true, string.Empty);
        });
    }

    public DataTable GetMachineLookupForCustomer()
    {
        EnsureInitialized();
        return _cloudStore.GetVendingMachinesLookup();
    }

    public DataTable GetMachineInventory(int machineId)
    {
        EnsureInitialized();
        return _cloudStore.GetMachineInventory(machineId);
    }

    public void SaveInventorySnapshot(int machineId, IEnumerable<Product> products)
    {
        EnsureInitialized();

        foreach (Product product in products)
        {
            if (product.DbInventoryId > 0)
            {
                _cloudStore.UpdateStock(product.DbInventoryId, product.Stock);
            }
        }

    }

    public void SaveInventoryItem(int machineId, Product product)
    {
        EnsureInitialized();

        if (product.DbInventoryId > 0)
        {
            _cloudStore.UpdateStock(product.DbInventoryId, product.Stock);
        }

    }

    public void LogEvent(int machineId, string eventType, string description, decimal amount = 0m)
    {
        EnsureInitialized();
        string clientSyncId = Guid.NewGuid().ToString();
        _cloudStore.InsertQueuedEventLog(clientSyncId, eventType, description, machineId, DateTime.UtcNow);
    }

    public void RecordSale(int machineId, int inventoryId, decimal amountPaid)
    {
        EnsureInitialized();
        _cloudStore.RecordSale(machineId, inventoryId, amountPaid);
    }

    public void SaveReceiptSession(Transaction transaction)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(transaction.ClientSyncId))
        {
            transaction.ClientSyncId = Guid.NewGuid().ToString();
        }

        _cloudStore.InsertQueuedReceiptSession(transaction);
    }
}
