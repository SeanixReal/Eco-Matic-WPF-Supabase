namespace Eco_Matic;

public interface IHasVolume
{
    int VolumeMl { get; }
}

public interface IHasCalories
{
    int Calories { get; }
}

public enum ProductType
{
    Snack,
    Drink,
    Misc
}

public abstract class VendingItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string FlavorText { get; set; } = "No description available.";
    public string ImagePath { get; set; } = string.Empty;
    public abstract ProductType Type { get; }

    public virtual string Examine() => FlavorText;
}
