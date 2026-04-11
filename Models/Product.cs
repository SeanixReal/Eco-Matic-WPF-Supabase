namespace Eco_Matic;

public class SnackItem : VendingItem, IHasCalories
{
    public int Calories { get; set; }
    
    public override ProductType Type => ProductType.Snack;

    public override string Examine() => $"{FlavorText} Calories: {Calories} kcal.";
}

public class DrinkItem : VendingItem, IHasCalories, IHasVolume
{
    public int Calories { get; set; }
    public int VolumeMl { get; set; }
    
    public override ProductType Type => ProductType.Drink;

    public override string Examine() => $"{FlavorText} Volume: {VolumeMl} ml.";
}

public class MiscItem : VendingItem
{
    public override ProductType Type => ProductType.Misc;
}
