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

    private static readonly List<VendingItem> DefaultProducts =
    [
        new SnackItem { Id = 1, Name = "Mr Chips", Price = 30.5m, Stock = 10, FlavorText = "Crunchy salted potato chips.", Calories = 160 },
        new SnackItem { Id = 2, Name = "Nova", Price = 40m, Stock = 10, FlavorText = "Cheesy square crackers.", Calories = 170 },
        new DrinkItem { Id = 3, Name = "Coca Cola", Price = 30.5m, Stock = 10, FlavorText = "Classic cola refreshment.", Calories = 140, VolumeMl = 330 },
        new DrinkItem { Id = 4, Name = "Pepsi", Price = 30m, Stock = 10, FlavorText = "Bold cola flavor.", Calories = 150, VolumeMl = 330 },
        new MiscItem { Id = 5, Name = "Bandaid Box", Price = 20m, Stock = 10, FlavorText = "Compact first-aid strips." },
        new MiscItem { Id = 6, Name = "Eco Bag", Price = 30.75m, Stock = 10, FlavorText = "Reusable eco-friendly carry bag." },
        new SnackItem { Id = 7, Name = "Piattos", Price = 35m, Stock = 10, FlavorText = "Sour cream and onion chips.", Calories = 180 },
        new SnackItem { Id = 8, Name = "Chippy", Price = 32m, Stock = 10, FlavorText = "Light and crispy chips.", Calories = 175 },
        new SnackItem { Id = 9, Name = "Roller Coaster", Price = 28.5m, Stock = 10, FlavorText = "Ridged chips with barbecue taste.", Calories = 165 },
        new SnackItem { Id = 10, Name = "Fudge Bar", Price = 25m, Stock = 10, FlavorText = "Chocolate coated wafer bar.", Calories = 150 },
        new SnackItem { Id = 11, Name = "Cheese Ring", Price = 30m, Stock = 10, FlavorText = "Cheesy ring-shaped corn snack.", Calories = 170 },
        new DrinkItem { Id = 12, Name = "RC Cola", Price = 25m, Stock = 10, FlavorText = "Refreshing RC cola.", Calories = 135, VolumeMl = 330 }
    ];

    public static List<VendingItem> Products { get; } = new();
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
