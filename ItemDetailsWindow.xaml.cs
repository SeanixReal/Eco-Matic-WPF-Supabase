using System.Windows;
using Eco_Matic.Utilities;

namespace Eco_Matic;

public partial class ItemDetailsWindow : Window
{
    public ItemDetailsWindow(IEnumerable<Product> matchingProducts)
    {
        InitializeComponent();
        Populate(matchingProducts.ToList());
    }

    private void Populate(IReadOnlyList<Product> matchingProducts)
    {
        if (matchingProducts.Count == 0)
        {
            txtItemName.Text = "Unknown Item";
            txtItemType.Text = "Type unavailable";
            txtPriceSummary.Text = "Price unavailable";
            txtStockSummary.Text = "No stock information available.";
            txtSlotSummary.Text = string.Empty;
            txtDescription.Text = "No description available.";
            return;
        }

        Product leadProduct = matchingProducts[0];
        decimal minPrice = matchingProducts.Min(product => product.Price);
        decimal maxPrice = matchingProducts.Max(product => product.Price);
        int totalStock = matchingProducts.Sum(product => product.Stock);
        string slotList = string.Join(", ", matchingProducts.Select(product => $"S{product.Id}"));

        txtItemName.Text = leadProduct.Name;
        txtItemType.Text = leadProduct.Type.ToString().ToUpperInvariant();
        txtPriceSummary.Text = minPrice == maxPrice
            ? $"Price: P{minPrice:F2}"
            : $"Price Range: P{minPrice:F2} - P{maxPrice:F2}";
        txtStockSummary.Text = $"Available Stock: {totalStock} item(s) across {matchingProducts.Count} slot(s)";
        txtSlotSummary.Text = $"Available in: {slotList}";
        txtDescription.Text = string.IsNullOrWhiteSpace(leadProduct.ExamineMessage)
            ? leadProduct.Examine()
            : leadProduct.ExamineMessage;
        imgItem.Source = ImageLoader.LoadProductImage(leadProduct.ImagePath);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void WindowFrame_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow
        {
            Owner = this
        };
        about.ShowDialog();
    }

    private void OpenReadmeMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var readme = new ReadmeWindow
        {
            Owner = this
        };
        readme.ShowDialog();
    }
}
