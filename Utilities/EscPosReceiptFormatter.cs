using System.Text;
using Eco_Matic.Data;

namespace Eco_Matic.Utilities;

public static class EscPosReceiptFormatter
{
    private const int PaperWidth = 32;
    private const byte FeedLinesBeforeCut = 2;

    public static byte[] BuildReceipt(Transaction transaction)
    {
        var bytes = new List<byte>();
        ReceiptProfile profile = ReceiptProfile.Load();
        string machineLabel = string.IsNullOrWhiteSpace(transaction.MachineDisplayName)
            ? $"Machine {transaction.MachineId}"
            : Clean(transaction.MachineDisplayName);
        string machineAddress = Clean(transaction.MachineAddress);

        Add(bytes, 0x1B, 0x40);
        Add(bytes, 0x1B, 0x74, 0x00);

        AddCenterLine(bytes, profile.CompanyName, emphasize: true, doubleSize: true);
        AddCenterLine(bytes, profile.BrandName, emphasize: true);
        AddCenterLine(bytes, profile.AddressLine);
        AddCenterLine(bytes, $"Tel: {profile.PhoneNumber}");
        AddCenterLine(bytes, profile.Slogan);
        AddLeftLine(bytes, Divider());
        AddLeftLine(bytes, $"Receipt No: {Clean(transaction.ReceiptNumber)}");
        AddLeftLine(bytes, $"Date: {transaction.SessionEndedAt:yyyy-MM-dd HH:mm}");
        AddLeftLine(bytes, "Cashier: SELF-SERVICE");
        AddLeftLine(bytes, $"Machine: {machineLabel}");
        foreach (string line in BuildIndentedLines("Address: ", machineAddress))
        {
            AddLeftLine(bytes, line);
        }
        AddLeftLine(bytes, Divider());

        foreach (string line in BuildItemLines(transaction))
        {
            AddLeftLine(bytes, line);
        }

        AddLeftLine(bytes, Divider());
        AddLeftLine(bytes, TwoColumn("Subtotal", transaction.TotalAmount.ToString("F2")));
        AddLeftLine(bytes, TwoColumn("Cash/QR Paid", transaction.AmountPaid.ToString("F2")));
        if (transaction.EcoPointsSpent > 0)
        {
            AddLeftLine(bytes, TwoColumn("Points Used", transaction.EcoPointsSpent.ToString()));
            AddLeftLine(bytes, TwoColumn(" Session Points", transaction.SessionPointsSpent.ToString()));
            AddLeftLine(bytes, TwoColumn(" Saved Credits", transaction.SavedEcoCreditsSpent.ToString()));
        }

        if (transaction.RecyclePointsTotal > 0)
        {
            AddLeftLine(bytes, TwoColumn("Points Earned", transaction.RecyclePointsTotal.ToString()));
        }

        AddLeftLine(bytes, TwoColumn("TOTAL", transaction.TotalAmount.ToString("F2")));
        AddLeftLine(bytes, TwoColumn("Change", transaction.Change.ToString("F2")));
        AddPointBalanceLines(bytes, transaction);
        AddLeftLine(bytes, Divider());
        AddCenterLine(bytes, "THANK YOU FOR BUYING!", emphasize: true);
        AddCenterLine(bytes, "PLEASE COME AGAIN");
        AddLeftLine(bytes, string.Empty);
        Add(bytes, 0x1B, 0x64, FeedLinesBeforeCut);
        Add(bytes, 0x1D, 0x56, 0x00);

        return bytes.ToArray();
    }

    public static string BuildReceiptText(Transaction transaction)
    {
        ReceiptProfile profile = ReceiptProfile.Load();
        string machineLabel = string.IsNullOrWhiteSpace(transaction.MachineDisplayName)
            ? $"Machine {transaction.MachineId}"
            : Clean(transaction.MachineDisplayName).ToUpperInvariant();
        string machineAddress = Clean(transaction.MachineAddress).ToUpperInvariant();

        var lines = new List<string>
        {
            Center(profile.CompanyName),
            Center(profile.BrandName),
            Center(profile.AddressLine),
            Center($"Tel: {profile.PhoneNumber}"),
            Center(profile.Slogan),
            Divider(),
            $"Receipt No: {Clean(transaction.ReceiptNumber)}",
            $"Date: {transaction.SessionEndedAt:yyyy-MM-dd HH:mm}",
            "Cashier: SELF-SERVICE",
            $"Machine: {machineLabel}"
        };

        lines.AddRange(BuildIndentedLines("Address: ", machineAddress));
        lines.Add(Divider());

        lines.AddRange(BuildItemLines(transaction));
        lines.Add(Divider());
        lines.Add(TwoColumn("Subtotal", transaction.TotalAmount.ToString("F2")));
        lines.Add(TwoColumn("Cash/QR Paid", transaction.AmountPaid.ToString("F2")));
        if (transaction.EcoPointsSpent > 0)
        {
            lines.Add(TwoColumn("Points Used", transaction.EcoPointsSpent.ToString()));
            lines.Add(TwoColumn(" Session Points", transaction.SessionPointsSpent.ToString()));
            lines.Add(TwoColumn(" Saved Credits", transaction.SavedEcoCreditsSpent.ToString()));
        }

        if (transaction.RecyclePointsTotal > 0)
        {
            lines.Add(TwoColumn("Points Earned", transaction.RecyclePointsTotal.ToString()));
        }

        lines.Add(TwoColumn("TOTAL", transaction.TotalAmount.ToString("F2")));
        lines.Add(TwoColumn("Change", transaction.Change.ToString("F2")));
        lines.AddRange(BuildPointBalanceLines(transaction));
        lines.Add(Divider());
        lines.Add(Center("THANK YOU FOR BUYING!"));
        lines.Add(Center("PLEASE COME AGAIN"));
        lines.Add(string.Empty);
        for (int i = 0; i < FeedLinesBeforeCut + 1; i++)
        {
            lines.Add(string.Empty);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Divider(char fill = '-') => new string(fill, PaperWidth);

    private static string Center(string text)
    {
        text = Clean(text);
        if (text.Length >= PaperWidth)
        {
            return text[..PaperWidth];
        }

        int leftPadding = (PaperWidth - text.Length) / 2;
        return new string(' ', leftPadding) + text;
    }

    private static IEnumerable<string> BuildItemLines(Transaction transaction)
    {
        if (transaction.Items.Count == 0 && transaction.RecycledItems.Count == 0)
        {
            yield return "No purchased items.";
            yield break;
        }

        foreach (var item in transaction.Items)
        {
            string itemName = Clean(item.ProductName);
            string pricing = $"{item.Quantity} x {item.UnitPrice:F2}";
            string total = item.LineTotal.ToString("F2");
            int availableNameWidth = Math.Max(8, PaperWidth - pricing.Length - total.Length - 2);
            string firstLineName = itemName.Length > availableNameWidth
                ? itemName[..availableNameWidth]
                : itemName;

            yield return TwoColumn($"{firstLineName}", $"{pricing} {total}");
            if (item.WasPaidWithPoints)
            {
                yield return $"  Paid with {item.PointsSpent} eco pts";
            }
            else if (item.CashPaid > 0m)
            {
                yield return $"  Paid with PHP {item.CashPaid:F2}";
            }

            if (itemName.Length > availableNameWidth)
            {
                foreach (string continuationLine in WrapText(itemName[availableNameWidth..], PaperWidth - 2))
                {
                    yield return $"  {continuationLine}";
                }
            }
        }

        foreach (var recycle in transaction.RecycledItems)
        {
            yield return TwoColumn($"Recycle {Clean(recycle.DisplayName)}", $"+{recycle.TotalPoints}");
        }
    }

    private static void AddPointBalanceLines(List<byte> bytes, Transaction transaction)
    {
        foreach (string line in BuildPointBalanceLines(transaction))
        {
            AddLeftLine(bytes, line);
        }
    }

    private static IEnumerable<string> BuildPointBalanceLines(Transaction transaction)
    {
        if (transaction.EcoCreditBalanceAfter.HasValue)
        {
            yield return TwoColumn("RFID Balance", $"{transaction.EcoCreditBalanceAfter.Value} pts");
            if (transaction.UnsavedSessionPointsRemaining > 0)
            {
                yield return TwoColumn("Points Pending", $"{transaction.UnsavedSessionPointsRemaining} pts");
            }

            yield break;
        }

        if (transaction.UnsavedSessionPointsRemaining > 0)
        {
            yield return TwoColumn("Points To Save", $"{transaction.UnsavedSessionPointsRemaining} pts");
        }
    }

    private static IEnumerable<string> BuildIndentedLines(string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        string cleanedValue = Clean(value);
        int firstLineWidth = Math.Max(6, PaperWidth - label.Length);
        List<string> wrappedLines = WrapText(cleanedValue, firstLineWidth).ToList();
        if (wrappedLines.Count == 0)
        {
            yield break;
        }

        yield return label + wrappedLines[0];
        string continuationIndent = new string(' ', label.Length);
        foreach (string continuation in wrappedLines.Skip(1))
        {
            yield return continuationIndent + continuation;
        }
    }

    private static string TwoColumn(string left, string right)
    {
        left = Clean(left);
        right = Clean(right);

        if (left.Length + right.Length + 1 > PaperWidth)
        {
            left = left[..Math.Max(1, Math.Min(left.Length, PaperWidth - right.Length - 1))];
        }

        int spaces = Math.Max(1, PaperWidth - left.Length - right.Length);
        return left + new string(' ', spaces) + right;
    }

    private static IEnumerable<string> WrapText(string text, int width)
    {
        text = Clean(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return string.Empty;
            yield break;
        }

        int index = 0;
        while (index < text.Length)
        {
            int length = Math.Min(width, text.Length - index);
            yield return text.Substring(index, length);
            index += length;
        }
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            builder.Append(ch <= 127 ? ch : '?');
        }

        return builder.ToString().Trim();
    }

    private static void AddLine(List<byte> bytes, string text)
    {
        bytes.AddRange(Encoding.ASCII.GetBytes(text));
        bytes.Add(0x0A);
    }

    private static void AddLeftLine(List<byte> bytes, string text, bool emphasize = false)
    {
        Add(bytes, 0x1B, 0x61, 0x00);
        Add(bytes, 0x1B, 0x45, emphasize ? (byte)0x01 : (byte)0x00);
        AddLine(bytes, text);
        if (emphasize)
        {
            Add(bytes, 0x1B, 0x45, 0x00);
        }
    }

    private static void AddCenterLine(List<byte> bytes, string text, bool emphasize = false, bool doubleSize = false)
    {
        Add(bytes, 0x1B, 0x61, 0x01);
        Add(bytes, 0x1B, 0x45, emphasize ? (byte)0x01 : (byte)0x00);
        Add(bytes, 0x1D, 0x21, doubleSize ? (byte)0x11 : (byte)0x00);
        AddLine(bytes, Clean(text));
        Add(bytes, 0x1B, 0x45, 0x00);
        Add(bytes, 0x1D, 0x21, 0x00);
    }

    private static void Add(List<byte> bytes, params byte[] command)
    {
        bytes.AddRange(command);
    }

    private sealed class ReceiptProfile
    {
        public string CompanyName { get; init; } = "LEAF SOLUTIONS";
        public string BrandName { get; init; } = "ECO-MATIC";
        public string AddressLine { get; init; } = "123 GREENWAY AVE, QUEZON CITY";
        public string PhoneNumber { get; init; } = "(02) 8555-0100";
        public string Slogan { get; init; } = "Smart vending for greener living";

        public static ReceiptProfile Load()
        {
            return new ReceiptProfile
            {
                CompanyName = Read("ECOMATIC_RECEIPT_COMPANY_NAME", "LEAF SOLUTIONS"),
                BrandName = Read("ECOMATIC_RECEIPT_BRAND_NAME", "ECO-MATIC"),
                AddressLine = Read("ECOMATIC_RECEIPT_ADDRESS", "123 GREENWAY AVE, QUEZON CITY"),
                PhoneNumber = Read("ECOMATIC_RECEIPT_PHONE", "(02) 8555-0100"),
                Slogan = Read("ECOMATIC_RECEIPT_SLOGAN", "Smart vending for greener living")
            };
        }

        private static string Read(string key, string fallback)
        {
            string? value = AppEnvironment.GetOptional(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
