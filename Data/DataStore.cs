namespace Eco_Matic;

public static class DataStore
{
    public const int MaxItemSlots = 12;
    public const int MaxStockPerItem = 15;

    public static readonly IReadOnlyDictionary<RecycleMaterial, decimal> RecycleRates =
        new Dictionary<RecycleMaterial, decimal>
        {
            [RecycleMaterial.Plastic] = 1.00m,
            [RecycleMaterial.Glass] = 2.00m,
            [RecycleMaterial.Aluminum] = 3.00m
        };

    private static readonly List<Product> DefaultProducts =
    [
        Product.Create(ProductType.Snack, 1, "Mr Chips", 30.5m, 10, "Crunchy salted potato chips.", 160),
        Product.Create(ProductType.Snack, 2, "Nova", 40m, 10, "Cheesy square crackers.", 170),
        Product.Create(ProductType.Drink, 3, "Coca Cola", 30.5m, 10, "Classic cola refreshment.", 140, 330),
        Product.Create(ProductType.Drink, 4, "Pepsi", 30m, 10, "Bold cola flavor.", 150, 330),
        Product.Create(ProductType.Misc, 5, "Bandaid Box", 20m, 10, "Compact first-aid strips."),
        Product.Create(ProductType.Misc, 6, "Eco Bag", 30.75m, 10, "Reusable eco-friendly carry bag."),
        Product.Create(ProductType.Snack, 7, "Piattos", 35m, 10, "Sour cream and onion chips.", 180),
        Product.Create(ProductType.Snack, 8, "Chippy", 32m, 10, "Light and crispy chips.", 175),
        Product.Create(ProductType.Snack, 9, "Roller Coaster", 28.5m, 10, "Ridged chips with barbecue taste.", 165),
        Product.Create(ProductType.Snack, 10, "Fudge Bar", 25m, 10, "Chocolate coated wafer bar.", 150),
        Product.Create(ProductType.Snack, 11, "Cheese Ring", 30m, 10, "Cheesy ring-shaped corn snack.", 170),
        Product.Create(ProductType.Drink, 12, "RC Cola", 25m, 10, "Refreshing RC cola.", 135, 330)
    ];

    public static List<Product> Products { get; } = new();
    public static List<Transaction> Transactions { get; } = new();
    public static int NextTransactionId { get; set; } = 1;
    public static Transaction? LastTransaction { get; set; }

    public static void Initialize()
    {
        Products.Clear();
        Products.AddRange(CsvStorage.LoadInventory(DefaultProducts));
        CsvStorage.EnsureEventLogFile();
        NextTransactionId = 1;
        LastTransaction = null;
        Transactions.Clear();
    }

    public static void SaveInventory() => CsvStorage.SaveInventory(Products);

    public static void LogEvent(string eventType, string details, decimal amount = 0m)
    {
        CsvStorage.AppendEvent(new EventLogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            EventType = eventType,
            Details = details,
            Amount = amount
        });
    }

    public static List<EventLogEntry> ReadLogs() => CsvStorage.LoadEventLog();

    public static void ClearLogs() => CsvStorage.ClearEventLog();
}
