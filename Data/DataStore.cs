namespace Eco_Matic;
using System;
using System.Collections.Generic;
using Eco_Matic.Utilities;

public static class DataStore
{
    public const int MaxItemSlots = 12;
    public const int MaxStockPerItem = 15;

    public static readonly IReadOnlyDictionary<RecycleMaterial, int> RecycleRates =
        new Dictionary<RecycleMaterial, int>
        {
            [RecycleMaterial.Plastic] = 1,
            [RecycleMaterial.Glass] = 2,
            [RecycleMaterial.Aluminum] = 3
        };

    public static List<Product> Products { get; } = new();
    public static List<Transaction> Transactions { get; } = new();
    public static int NextTransactionId { get; set; } = 1;
    public static Transaction? LastTransaction { get; set; }
    public static int ActiveMachineId { get; set; } = 1;
    public static int PendingPoints { get; set; } = 0;

    public static void Initialize(int machineId = 1)
    {
        ActiveMachineId = machineId;
        PendingPoints = 0;
        Products.Clear();
        var store = new Eco_Matic.Data.SupabaseStore();
        
        var dt = store.GetMachineInventory(machineId);

        foreach (System.Data.DataRow row in dt.Rows)
        {
            string rawSlotId = row["Slot"].ToString() ?? "";
            if (!SlotIdHelper.TryGetSlotNumber(rawSlotId, out int slotNumber))
            {
                continue;
            }

            int inventoryId = Convert.ToInt32(row["_InventoryID"]);
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

            var p = Product.Create(pType, slotNumber, name, price, stock, examineMessage, calories, 0, imagePath, dispenseMessage, examineMessage);
            p.DbInventoryId = inventoryId; // Use DbInventoryId for SQL ops if tracking ID. But UI uses slotIndex.
            Products.Add(p);
        }
        
        NextTransactionId = 1;
        LastTransaction = null;
        Transactions.Clear();
    }

    public static void SaveInventory()
    {
        // Now synced live with DB, flush the current stock.
        var store = new Eco_Matic.Data.SupabaseStore();
        foreach (var p in Products)
        {
            if (p.DbInventoryId > 0)
            {
                store.UpdateStock(p.DbInventoryId, p.Stock);
            }
        }
    }

    public static void LogEvent(string eventType, string details, decimal amount = 0m)
    {
        var store = new Eco_Matic.Data.SupabaseStore();
        store.LogEvent(eventType, details, amount, ActiveMachineId);
    }

    public static void RecordSale(int inventoryId, decimal amountPaid)
    {
        // Actually needs the current machine ID.
        // If we only have 1 active machine right now, or if we track ActiveMachineId in DataStore
        var store = new Eco_Matic.Data.SupabaseStore();
        store.RecordSale(ActiveMachineId, inventoryId, amountPaid);
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
}
