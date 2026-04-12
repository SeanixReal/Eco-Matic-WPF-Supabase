namespace Eco_Matic;

public class Product : VendingItem
{
    public override ProductType Type { get; }

    public Product(ProductType type)
    {
        Type = type;
    }

    public static Product Create(
        ProductType type,
        int id,
        string name,
        decimal price,
        int stock,
        string flavorText,
        int calories = 0,
        int volumeMl = 0,
        string imagePath = "",
        string dispenseMessage = "Enjoy your item!",
        string examineMessage = "A standard vending item.")
    {
        return type switch
        {
            ProductType.Drink => new DrinkItem
            {
                Id = id,
                Name = name,
                Price = price,
                Stock = stock,
                FlavorText = flavorText,
                Calories = calories,
                VolumeMl = volumeMl,
                ImagePath = imagePath,
                DispenseMessage = dispenseMessage,
                ExamineMessage = examineMessage
            },
            ProductType.Snack => new SnackItem
            {
                Id = id,
                Name = name,
                Price = price,
                Stock = stock,
                FlavorText = flavorText,
                Calories = calories,
                ImagePath = imagePath,
                DispenseMessage = dispenseMessage,
                ExamineMessage = examineMessage
            },
            _ => new MiscItem
            {
                Id = id,
                Name = name,
                Price = price,
                Stock = stock,
                FlavorText = flavorText,
                ImagePath = imagePath,
                DispenseMessage = dispenseMessage,
                ExamineMessage = examineMessage
            }
        };
    }
}

public class SnackItem : Product, IHasCalories
{
    public int Calories { get; set; }

    public SnackItem() : base(ProductType.Snack)
    {
    }

    public override string Examine() => $"{FlavorText} Calories: {Calories} kcal.";
}

public class DrinkItem : Product, IHasCalories, IHasVolume
{
    public int Calories { get; set; }
    public int VolumeMl { get; set; }

    public DrinkItem() : base(ProductType.Drink)
    {
    }

    public override string Examine() => $"{FlavorText} Volume: {VolumeMl} ml.";
}

public class MiscItem : Product
{
    public MiscItem() : base(ProductType.Misc)
    {
    }
}
