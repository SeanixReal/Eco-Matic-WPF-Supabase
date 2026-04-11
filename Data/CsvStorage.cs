using System.Globalization;
using System.IO;
using System.Text;

namespace Eco_Matic;

public static class CsvStorage
{
    private const string InventoryHeader = "Id,Type,Name,Price,Stock,FlavorText,Calories,VolumeMl,ImagePath";
    private const string EventLogHeader = "TimestampUtc,EventType,Details,Amount";

    private static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "data");
    public static string ImageDirectory => Path.Combine(DataDirectory, "images");
    private static string InventoryPath => Path.Combine(DataDirectory, "inventory.csv");
    private static string EventLogPath => Path.Combine(DataDirectory, "eventLog.csv");

    public static List<Product> LoadInventory(List<Product> fallback)
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(ImageDirectory);

        if (!File.Exists(InventoryPath))
        {
            SaveInventory(fallback);
            return CloneProducts(fallback);
        }

        var lines = File.ReadAllLines(InventoryPath);
        if (lines.Length <= 1)
        {
            SaveInventory(fallback);
            return CloneProducts(fallback);
        }

        var products = new List<Product>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var fields = ParseLine(lines[i]);
            if (fields.Count < 8)
            {
                continue;
            }

            if (!int.TryParse(fields[0], out int id))
            {
                continue;
            }

            if (!Enum.TryParse(fields[1], true, out ProductType type))
            {
                type = ProductType.Misc;
            }

            if (!decimal.TryParse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price))
            {
                continue;
            }

            if (!int.TryParse(fields[4], out int stock))
            {
                continue;
            }

            _ = int.TryParse(fields[6], out int calories);
            _ = int.TryParse(fields[7], out int volumeMl);

            string imagePath = fields.Count > 8 ? fields[8] : string.Empty;

            stock = Math.Clamp(stock, 0, DataStore.MaxStockPerItem);

            products.Add(Product.Create(
                type,
                id,
                fields[2],
                price,
                stock,
                fields[5],
                calories,
                volumeMl,
                imagePath));
        }

        if (products.Count == 0)
        {
            SaveInventory(fallback);
            return CloneProducts(fallback);
        }

        return products
            .OrderBy(p => p.Id)
            .Take(DataStore.MaxItemSlots)
            .ToList();
    }

    public static void SaveInventory(IEnumerable<Product> products)
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(ImageDirectory);
        using var writer = new StreamWriter(InventoryPath, false, Encoding.UTF8);
        writer.WriteLine(InventoryHeader);

        foreach (var product in products.OrderBy(p => p.Id))
        {
            int calories = product is IHasCalories withCalories ? withCalories.Calories : 0;
            int volumeMl = product is IHasVolume withVolume ? withVolume.VolumeMl : 0;

            writer.WriteLine(string.Join(",",
                product.Id,
                product.Type,
                Escape(product.Name),
                product.Price.ToString("0.00", CultureInfo.InvariantCulture),
                product.Stock,
                Escape(product.FlavorText),
                calories,
                volumeMl,
                Escape(product.ImagePath)));
        }
    }

    public static void EnsureEventLogFile()
    {
        Directory.CreateDirectory(DataDirectory);
        if (!File.Exists(EventLogPath))
        {
            File.WriteAllText(EventLogPath, EventLogHeader + Environment.NewLine);
        }
    }

    public static List<EventLogEntry> LoadEventLog()
    {
        EnsureEventLogFile();
        var result = new List<EventLogEntry>();
        var lines = File.ReadAllLines(EventLogPath);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var fields = ParseLine(lines[i]);
            if (fields.Count < 4)
            {
                continue;
            }

            if (!DateTime.TryParse(fields[0], null, DateTimeStyles.AdjustToUniversal, out DateTime ts))
            {
                continue;
            }

            if (!decimal.TryParse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
            {
                amount = 0m;
            }

            result.Add(new EventLogEntry
            {
                TimestampUtc = ts,
                EventType = fields[1],
                Details = fields[2],
                Amount = amount
            });
        }

        return result.OrderByDescending(e => e.TimestampUtc).ToList();
    }

    public static void AppendEvent(EventLogEntry entry)
    {
        EnsureEventLogFile();
        string line = string.Join(",",
            entry.TimestampUtc.ToString("O"),
            Escape(entry.EventType),
            Escape(entry.Details),
            entry.Amount.ToString("0.00", CultureInfo.InvariantCulture));
        File.AppendAllText(EventLogPath, line + Environment.NewLine);
    }

    public static void ClearEventLog()
    {
        EnsureEventLogFile();
        File.WriteAllText(EventLogPath, EventLogHeader + Environment.NewLine);
    }

    public static string CopyProductImage(string sourceFilePath, int productId)
    {
        Directory.CreateDirectory(ImageDirectory);
        string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
        string destFileName = $"{productId}{ext}";
        string destPath = Path.Combine(ImageDirectory, destFileName);

        File.Copy(sourceFilePath, destPath, overwrite: true);
        return destFileName;
    }

    public static string GetImageFullPath(string relativeFileName)
    {
        if (string.IsNullOrWhiteSpace(relativeFileName))
        {
            return string.Empty;
        }

        return Path.Combine(ImageDirectory, relativeFileName);
    }

    private static List<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        fields.Add(sb.ToString());
        return fields;
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        return value;
    }

    private static List<Product> CloneProducts(IEnumerable<Product> source)
    {
        return source
            .Select(p => Product.Create(
                p.Type,
                p.Id,
                p.Name,
                p.Price,
                p.Stock,
                p.FlavorText,
                p is IHasCalories c ? c.Calories : 0,
                p is IHasVolume v ? v.VolumeMl : 0,
                p.ImagePath))
            .ToList();
    }
}
