namespace Eco_Matic;

public enum RecycleMaterial
{
    Plastic,
    Glass,
    Aluminum
}

public class RecycleEntry
{
    public RecycleMaterial Material { get; set; }
    public int Pieces { get; set; }
    public int PointsPerPiece { get; set; }
    public int TotalPoints => Pieces * PointsPerPiece;
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
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class Transaction
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public List<TransactionItem> Items { get; set; } = new();
    public List<RecycleEntry> RecycledItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
}
