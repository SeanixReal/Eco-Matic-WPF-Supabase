using System.Linq;

namespace Eco_Matic;

public class RecycleEntry
{
    public int RecyclableItemId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = "piece";
    public int Pieces { get; set; }
    public int PointsPerUnit { get; set; }
    public string Description { get; set; } = string.Empty;
    public int TotalPoints => Pieces * PointsPerUnit;
}

public class EventLogEntry
{
    public DateTime TimestampUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TransactionItem
{
    public int ProductId { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class Transaction
{
    public int Id { get; set; }
    public string ClientSyncId { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public int MachineId { get; set; }
    public string MachineDisplayName { get; set; } = string.Empty;
    public string MachineAddress { get; set; } = string.Empty;
    public DateTime SessionStartedAt { get; set; }
    public DateTime SessionEndedAt { get; set; }
    public DateTime Date { get; set; }
    public List<TransactionItem> Items { get; set; } = new();
    public List<RecycleEntry> RecycledItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public string Source { get; set; } = "online";
    public int RecyclePointsTotal => RecycledItems.Sum(entry => entry.TotalPoints);
    public bool HasActivity => Items.Count > 0 || RecycledItems.Count > 0 || AmountPaid > 0m || Change > 0m;
}
