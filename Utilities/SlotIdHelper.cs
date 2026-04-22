using System.Globalization;
using System.Text;

namespace Eco_Matic.Utilities;

public static class SlotIdHelper
{
    public const int MinSlot = 1;
    public const int MaxSlot = 12;

    public static string? Normalize(string? slotId)
    {
        if (!TryGetSlotNumber(slotId, out int slotNumber))
        {
            return null;
        }

        return slotNumber.ToString(CultureInfo.InvariantCulture);
    }

    public static bool TryGetSlotNumber(string? slotId, out int slotNumber)
    {
        slotNumber = 0;
        if (string.IsNullOrWhiteSpace(slotId))
        {
            return false;
        }

        string trimmed = slotId.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out slotNumber))
        {
            return slotNumber is >= MinSlot and <= MaxSlot;
        }

        var digits = new StringBuilder();
        foreach (char ch in trimmed)
        {
            if (char.IsDigit(ch))
            {
                digits.Append(ch);
            }
        }

        if (digits.Length == 0)
        {
            return false;
        }

        return int.TryParse(digits.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out slotNumber)
            && slotNumber is >= MinSlot and <= MaxSlot;
    }
}
