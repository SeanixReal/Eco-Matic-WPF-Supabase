namespace Eco_Matic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Eco_Matic.Data;
using Eco_Matic.Utilities;

public static class DataStore
{
    public const int MaxItemSlots = 12;
    public const int MaxStockPerItem = 15;
    private static readonly Eco_Matic.Data.SupabaseSessionCoordinator SupabaseSession = Eco_Matic.Data.SupabaseSessionCoordinator.Instance;

    public static List<Product> Products { get; } = new();
    public static List<RecyclableItemDefinition> RecyclableItems { get; } = new();
    public static List<Transaction> Transactions { get; } = new();
    private static int _nextTransactionId = 1;
    public static int NextTransactionId
    {
        get => _nextTransactionId;
        set => _nextTransactionId = value;
    }
    public static Transaction? LastTransaction { get; set; }
    public static int ActiveMachineId { get; set; } = 1;
    public static string ActiveMachineDisplayName { get; set; } = "Main Lobby";
    public static string ActiveMachineAddress { get; set; } = string.Empty;
    public static int PendingPoints { get; set; } = 0;

    public static bool Initialize(int machineId = 1)
    {
        ActiveMachineId = machineId;
        if (string.IsNullOrWhiteSpace(ActiveMachineDisplayName))
        {
            ActiveMachineDisplayName = $"Machine {machineId}";
        }

        PendingPoints = 0;
        if (!TryLoadProducts(machineId, out List<Product> loadedProducts))
        {
            return false;
        }

        Products.Clear();
        Products.AddRange(loadedProducts);
        
        NextTransactionId = 1;
        LastTransaction = null;
        Transactions.Clear();
        return true;
    }

    public static int AllocateTransactionId()
    {
        return Interlocked.Increment(ref _nextTransactionId) - 1;
    }

    public static void SaveInventory()
    {
        SupabaseSession.SaveInventorySnapshot(ActiveMachineId, Products);
    }

    public static void SaveInventory(Product product)
    {
        SupabaseSession.SaveInventoryItem(ActiveMachineId, product);
    }

    public static void SaveInventory(int machineId, Product product)
    {
        SupabaseSession.SaveInventoryItem(machineId, product);
    }

    public static bool RefreshActiveMachineInventory()
    {
        if (!TryLoadProducts(ActiveMachineId, out List<Product> refreshedProducts))
        {
            return false;
        }

        Products.Clear();
        Products.AddRange(refreshedProducts);
        return true;
    }

    public static bool TryGetProductsForMachine(int machineId, out List<Product> products)
    {
        return TryLoadProducts(machineId, out products);
    }

    public static void LogEvent(string eventType, string details, decimal amount = 0m)
    {
        SupabaseSession.LogEvent(ActiveMachineId, eventType, details, amount);
    }

    public static void LogEvent(int machineId, string eventType, string details, decimal amount = 0m)
    {
        SupabaseSession.LogEvent(machineId, eventType, details, amount);
    }

    public static void RecordSale(int inventoryId, decimal amountPaid)
    {
        SupabaseSession.RecordSale(ActiveMachineId, inventoryId, amountPaid);
    }

    public static void RecordSale(int machineId, int inventoryId, decimal amountPaid)
    {
        SupabaseSession.RecordSale(machineId, inventoryId, amountPaid);
    }

    public static void SaveCompletedReceipt(Transaction transaction)
    {
        Transactions.Add(transaction);
        LastTransaction = transaction;
        SupabaseSession.SaveReceiptSession(transaction);
    }

    public static List<EventLogEntry> ReadLogs()
    {
        var store = new Eco_Matic.Data.SupabaseStore();
        var dt = store.GetEventLogs();
        var list = new List<EventLogEntry>();
        foreach (System.Data.DataRow r in dt.Rows)
        {
            list.Add(new EventLogEntry
            {
                TimestampUtc = Convert.ToDateTime(r["Timestamp"]),
                EventType = r["Event"].ToString() ?? "",
                Details = r["Notes"].ToString() ?? ""
            });
        }
        return list;
    }

    public static void ClearLogs()
    {
        var store = new Eco_Matic.Data.SupabaseStore();
        store.ClearEventLogs();
    }

    public static List<RecyclableItemDefinition> GetFallbackRecyclableCatalog()
    {
        return
        [
            new RecyclableItemDefinition
            {
                Id = 1,
                DisplayName = "Plastic Bottle",
                MaterialType = "PET Plastic",
                UnitLabel = "bottle",
                PointsPerUnit = 2,
                Description = "Clean PET beverage bottles such as water, juice, or soda bottles.",
                IsActive = true,
                SortOrder = 1
            },
            new RecyclableItemDefinition
            {
                Id = 2,
                DisplayName = "Aluminum Can",
                MaterialType = "Aluminum",
                UnitLabel = "can",
                PointsPerUnit = 3,
                Description = "Used soda or juice cans made from aluminum.",
                IsActive = true,
                SortOrder = 2
            },
            new RecyclableItemDefinition
            {
                Id = 3,
                DisplayName = "Glass Bottle",
                MaterialType = "Glass",
                UnitLabel = "bottle",
                PointsPerUnit = 4,
                Description = "Empty glass beverage bottles with no liquid remaining.",
                IsActive = true,
                SortOrder = 3
            },
            new RecyclableItemDefinition
            {
                Id = 4,
                DisplayName = "Plastic Cup",
                MaterialType = "Plastic",
                UnitLabel = "cup",
                PointsPerUnit = 1,
                Description = "Disposable plastic drink cups that are dry and empty.",
                IsActive = true,
                SortOrder = 4
            },
            new RecyclableItemDefinition
            {
                Id = 5,
                DisplayName = "Detergent Pouch",
                MaterialType = "Flexible Plastic",
                UnitLabel = "pouch",
                PointsPerUnit = 1,
                Description = "Clean detergent or refill pouches with contents removed.",
                IsActive = true,
                SortOrder = 5
            },
            new RecyclableItemDefinition
            {
                Id = 6,
                DisplayName = "Tin Can",
                MaterialType = "Steel",
                UnitLabel = "can",
                PointsPerUnit = 2,
                Description = "Food cans made from tin-coated steel, rinsed and empty.",
                IsActive = true,
                SortOrder = 6
            }
        ];
    }

    public static bool RefreshRecyclableCatalog()
    {
        try
        {
            List<RecyclableItemDefinition> refreshedItems;
            var store = new Eco_Matic.Data.SupabaseStore();
            refreshedItems = store.GetActiveRecyclableItems();
            if (refreshedItems.Count == 0)
            {
                refreshedItems = GetFallbackRecyclableCatalog();
            }

            RecyclableItems.Clear();
            RecyclableItems.AddRange(refreshedItems.OrderBy(item => item.SortOrder).ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase));
            return RecyclableItems.Count > 0;
        }
        catch
        {
            RecyclableItems.Clear();
            RecyclableItems.AddRange(GetFallbackRecyclableCatalog());
            return RecyclableItems.Count > 0;
        }
    }

    private static bool TryLoadProducts(int machineId, out List<Product> loadedProducts)
    {
        loadedProducts = new List<Product>();
        var dt = SupabaseSession.GetMachineInventory(machineId);

        if (dt.Rows.Count == 0)
        {
            return false;
        }

        foreach (System.Data.DataRow row in dt.Rows)
        {
            string rawSlotId = row["Slot"].ToString() ?? "";
            if (!SlotIdHelper.TryGetSlotNumber(rawSlotId, out int slotNumber))
            {
                continue;
            }

            int inventoryId = Convert.ToInt32(row["_InventoryID"]);
            int itemId = Convert.ToInt32(row["_ItemID"]);
            string name = row["Item"].ToString() ?? "Unknown";
            string typeStr = row["Type"].ToString() ?? "Misc";
            decimal price = Convert.ToDecimal(row["Price"]);
            int stock = Convert.ToInt32(row["Stock"]);
            int calories = row["Calories"] != DBNull.Value ? Convert.ToInt32(row["Calories"]) : 0;
            string imagePath = row["Image"].ToString() ?? "";
            string dispenseMessage = row["Dispense Message"]?.ToString() ?? "Enjoy your item!";
            string examineMessage = row["Examine Message"]?.ToString() ?? "A standard vending item.";

            ProductType pType = ProductType.Misc;
            if (Enum.TryParse<ProductType>(typeStr, out var parsedType))
            {
                pType = parsedType;
            }

            var product = Product.Create(pType, slotNumber, name, price, stock, examineMessage, calories, 0, imagePath, dispenseMessage, examineMessage);
            product.DbInventoryId = inventoryId;
            product.CatalogItemId = itemId;
            loadedProducts.Add(product);
        }

        return loadedProducts.Count > 0;
    }
}
