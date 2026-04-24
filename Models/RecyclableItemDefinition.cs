namespace Eco_Matic;

public class RecyclableItemDefinition
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string UnitLabel { get; set; } = "piece";
    public int PointsPerUnit { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public string CustomerLabel
    {
        get
        {
            string pointLabel = PointsPerUnit == 1 ? "point" : "points";
            string unit = string.IsNullOrWhiteSpace(UnitLabel) ? "item" : UnitLabel.Trim();
            return $"{DisplayName} - {PointsPerUnit} {pointLabel} per {unit}";
        }
    }

    public override string ToString() => CustomerLabel;
}
