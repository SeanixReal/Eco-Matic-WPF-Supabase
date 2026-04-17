using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Eco_Matic;

public partial class CustomerWindow : Window
{
    private readonly List<RecycleEntry> _recycleEntries = new();
    private readonly Dictionary<int, SlotControls> _slots = new();

    private decimal _insertedMoney;
    private bool _isDispensing;
    private readonly Data.ArduinoService? _arduino;
    private DispatcherTimer? _dispenseTimer;
    private DispatcherTimer? _blinkTimer;
    private int _blinkSlotId;
    private int _blinkCount;

    private static readonly Brush SlotDefault = CreateBrush(249, 251, 254);
    private static readonly Brush SlotEmpty = CreateBrush(238, 243, 249);
    private static readonly Brush SlotAlert = CreateBrush(247, 197, 197);
    private static readonly Brush TextBright = CreateBrush(35, 50, 77);
    private static readonly Brush TextDim = CreateBrush(129, 144, 168);
    private static readonly Brush PriceCyan = CreateBrush(46, 119, 230);
    private static readonly Brush SoldOutRed = CreateBrush(214, 90, 90);
    private static readonly Brush ButtonReady = CreateBrush(47, 166, 106);
    private static readonly Brush ButtonDim = CreateBrush(238, 243, 252);
    private static readonly Brush ButtonSold = CreateBrush(247, 219, 219);
    private static readonly Brush StatusIdle = CreateBrush(74, 100, 141);

    private readonly record struct SlotControls(
        Border Panel,
        Image VendingItemImage,
        TextBlock NameLabel,
        TextBlock PriceLabel,
        Button SelectButton);

    private sealed class VendingItemOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public CustomerWindow(Data.ArduinoService? arduino = null)
    {
        InitializeComponent();
        _arduino = arduino;
        InitializeSlots();
        InitializeSelectors();
        RefreshProducts();
        UpdateMoneyDisplay();
        SetDispenseStatus("INSERT MONEY TO START", StatusIdle);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        if (_dispenseTimer != null)
        {
            _dispenseTimer.Stop();
            _dispenseTimer = null;
        }

        if (_blinkTimer != null)
        {
            _blinkTimer.Stop();
            _blinkTimer = null;
        }
    }

    private void InitializeSlots()
    {
        AddSlot(1, pnlSlot1, imgSlot1, lblName1, lblPrice1, btnSel1);
        AddSlot(2, pnlSlot2, imgSlot2, lblName2, lblPrice2, btnSel2);
        AddSlot(3, pnlSlot3, imgSlot3, lblName3, lblPrice3, btnSel3);
        AddSlot(4, pnlSlot4, imgSlot4, lblName4, lblPrice4, btnSel4);
        AddSlot(5, pnlSlot5, imgSlot5, lblName5, lblPrice5, btnSel5);
        AddSlot(6, pnlSlot6, imgSlot6, lblName6, lblPrice6, btnSel6);
        AddSlot(7, pnlSlot7, imgSlot7, lblName7, lblPrice7, btnSel7);
        AddSlot(8, pnlSlot8, imgSlot8, lblName8, lblPrice8, btnSel8);
        AddSlot(9, pnlSlot9, imgSlot9, lblName9, lblPrice9, btnSel9);
        AddSlot(10, pnlSlot10, imgSlot10, lblName10, lblPrice10, btnSel10);
        AddSlot(11, pnlSlot11, imgSlot11, lblName11, lblPrice11, btnSel11);
        AddSlot(12, pnlSlot12, imgSlot12, lblName12, lblPrice12, btnSel12);
    }

    private void AddSlot(
        int id,
        Border panel,
        Image productImage,
        TextBlock nameLabel,
        TextBlock priceLabel,
        Button selectButton)
    {
        selectButton.Tag = id;
        _slots[id] = new SlotControls(panel, productImage, nameLabel, priceLabel, selectButton);
    }

    private void InitializeSelectors()
    {
        cboRecycleType.Items.Clear();
        foreach (var material in Enum.GetValues<RecycleMaterial>())
        {
            cboRecycleType.Items.Add($"{material} ({DataStore.RecycleRates[material]} pts/pc)");
        }

        if (cboRecycleType.Items.Count > 0)
        {
            cboRecycleType.SelectedIndex = 0;
        }

        txtRecycleQty.Text = "1";
        RefreshExamineOptions();
    }

    private void RefreshProducts()
    {
        foreach (var slotPair in _slots)
        {
            int slotId = slotPair.Key;
            SlotControls slot = slotPair.Value;

            var product = DataStore.Products.FirstOrDefault(p => p.Id == slotId);
            if (product == null)
            {
                slot.NameLabel.Text = "EMPTY";
                slot.NameLabel.Foreground = TextDim;
                slot.PriceLabel.Text = string.Empty;
                slot.Panel.Background = SlotEmpty;

                slot.SelectButton.IsEnabled = false;
                slot.SelectButton.Content = "--";
                slot.SelectButton.Background = ButtonDim;
                slot.SelectButton.Foreground = TextDim;
                slot.SelectButton.BorderBrush = CreateBrush(201, 216, 239);

                slot.VendingItemImage.Source = null;
                continue;
            }

            slot.VendingItemImage.Source = ImageLoader.LoadProductImage(product.ImagePath);

            if (product.Stock > 0)
            {
                slot.NameLabel.Text = product.Name;
                slot.NameLabel.Foreground = TextBright;
                slot.PriceLabel.Text = $"P{product.Price:F2}  x{product.Stock}";
                slot.PriceLabel.Foreground = PriceCyan;
                slot.Panel.Background = SlotDefault;

                slot.SelectButton.IsEnabled = true;
                slot.SelectButton.Content = "SELECT";
                UpdateButtonBuyability(slot, product);
            }
            else
            {
                slot.NameLabel.Text = product.Name;
                slot.NameLabel.Foreground = TextDim;
                slot.PriceLabel.Text = "OUT OF STOCK";
                slot.PriceLabel.Foreground = SoldOutRed;
                slot.Panel.Background = SlotEmpty;

                slot.SelectButton.IsEnabled = false;
                slot.SelectButton.Content = "OUT OF STOCK";
                slot.SelectButton.Background = ButtonSold;
                slot.SelectButton.Foreground = CreateBrush(133, 57, 57);
                slot.SelectButton.BorderBrush = CreateBrush(223, 163, 163);
            }
        }

        RefreshExamineOptions();
    }

    private void UpdateAllButtonStates()
    {
        foreach (var slotPair in _slots)
        {
            var product = DataStore.Products.FirstOrDefault(p => p.Id == slotPair.Key);
            if (product is { Stock: > 0 })
            {
                UpdateButtonBuyability(slotPair.Value, product);
            }
        }
    }

    private void UpdateButtonBuyability(SlotControls slot, VendingItem product)
    {
        if (_insertedMoney >= product.Price)
        {
            slot.SelectButton.Background = ButtonReady;
            slot.SelectButton.Foreground = Brushes.White;
            slot.SelectButton.BorderBrush = CreateBrush(47, 166, 106);
        }
        else
        {
            slot.SelectButton.Background = ButtonDim;
            slot.SelectButton.Foreground = CreateBrush(48, 65, 92);
            slot.SelectButton.BorderBrush = CreateBrush(201, 216, 239);
        }
    }

    private void RefreshExamineOptions()
    {
        int? selectedId = (cboExamineItem.SelectedItem as VendingItemOption)?.Id;

        cboExamineItem.Items.Clear();
        foreach (var p in DataStore.Products.OrderBy(p => p.Id))
        {
            cboExamineItem.Items.Add(new VendingItemOption
            {
                Id = p.Id,
                Name = p.Name
            });
        }

        if (selectedId.HasValue)
        {
            var same = cboExamineItem.Items.OfType<VendingItemOption>().FirstOrDefault(x => x.Id == selectedId.Value);
            if (same != null)
            {
                cboExamineItem.SelectedItem = same;
                return;
            }
        }

        if (cboExamineItem.Items.Count > 0)
        {
            cboExamineItem.SelectedIndex = 0;
        }
    }

    private void UpdateMoneyDisplay()
    {
        lblMoneyAmount.Text = $"P {_insertedMoney:F2} | Pts: {DataStore.PendingPoints}";
    }

    private void BtnMoney_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (!int.TryParse(button.Tag?.ToString(), out int amount))
        {
            return;
        }

        _insertedMoney += amount;
        UpdateMoneyDisplay();
        lblRecycleStatus.Text = "Balance updated";
        UpdateAllButtonStates();
    }

    private void BtnCoinReturn_Click(object sender, RoutedEventArgs e)
    {
        if (_insertedMoney <= 0)
        {
            return;
        }

        decimal returned = _insertedMoney;
        _insertedMoney = 0;
        _recycleEntries.Clear();

        UpdateMoneyDisplay();
        lblRecycleStatus.Text = "Balance reset";
        SetDispenseStatus("INSERT MONEY TO START", StatusIdle);
        UpdateAllButtonStates();

        MessageBox.Show(this,
            $"P{returned:F2} returned. Please collect your money.",
            "Coin Return",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnRecycle_Click(object sender, RoutedEventArgs e)
    {
        if (cboRecycleType.SelectedIndex < 0)
        {
            return;
        }

        var material = (RecycleMaterial)cboRecycleType.SelectedIndex;

        if (!int.TryParse(txtRecycleQty.Text, out int pieces) || pieces <= 0)
        {
            MessageBox.Show(this,
                "Enter a valid number of items (pieces) greater than zero.",
                "Recycle Credit",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        int ratePerPiece = DataStore.RecycleRates[material];
        int points = ratePerPiece * pieces;
        DataStore.PendingPoints += points;

        var existing = _recycleEntries.FirstOrDefault(x => x.Material == material);
        if (existing == null)
        {
            _recycleEntries.Add(new RecycleEntry
            {
                Material = material,
                Pieces = pieces,
                PointsPerPiece = ratePerPiece
            });
        }
        else
        {
            existing.Pieces += pieces;
        }

        DataStore.LogEvent("RECYCLE", $"{pieces} pc(s) {material}", points);
        lblRecycleStatus.Text = $"+{points} Pts";
        UpdateMoneyDisplay();
        UpdateAllButtonStates();
    }

    private void BtnExamine_Click(object sender, RoutedEventArgs e)
    {
        if (cboExamineItem.SelectedItem is not VendingItemOption option)
        {
            return;
        }

        var product = DataStore.Products.FirstOrDefault(p => p.Id == option.Id);
        if (product == null)
        {
            RefreshProducts();
            return;
        }

        MessageBox.Show(this,
            $"{product.Name}\n" +
            $"Type: {product.Type}\n" +
            $"Price: P{product.Price:F2}\n" +
            $"Stock: {product.Stock}\n\n" +
            product.Examine(),
            "Item Details",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isDispensing)
        {
            return;
        }

        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out int slotId))
        {
            return;
        }

        var product = DataStore.Products.FirstOrDefault(p => p.Id == slotId);
        if (product == null)
        {
            return;
        }

        if (product.Stock <= 0)
        {
            SetDispenseStatus("Item is sold out.", Brushes.IndianRed);
            return;
        }

        if (_insertedMoney < product.Price)
        {
            StartBlink(slotId);
            SetDispenseStatus("NOT ENOUGH MONEY", SoldOutRed);
            return;
        }

        _insertedMoney -= product.Price;
        product.Stock--;
        DataStore.SaveInventory();

        var transaction = CreateTransaction(product);
        DataStore.Transactions.Add(transaction);
        DataStore.LastTransaction = transaction;
        
        string logDetails = $"Item: {product.Name} | Quantity: 1 | Price: ₱{product.Price:0.00} | Total: ₱{product.Price:0.00}";
        DataStore.LogEvent("PURCHASE", logDetails, product.Price);
        DataStore.RecordSale(product.DbInventoryId, product.Price);

        btnBack.Content = "DONE & RECEIPT";
        btnBack.Background = new SolidColorBrush(Color.FromRgb(47, 166, 106));

        StartDispenseFeedback(product);
        UpdateMoneyDisplay();
        RefreshProducts();
    }

    private Transaction CreateTransaction(VendingItem product)
    {
        var transaction = new Transaction
        {
            Id = DataStore.NextTransactionId++,
            Date = DateTime.Now,
            TotalAmount = product.Price,
            AmountPaid = product.Price,
            Change = 0
        };

        transaction.Items.Add(new TransactionItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = 1,
            UnitPrice = product.Price
        });

        foreach (var entry in _recycleEntries)
        {
            transaction.RecycledItems.Add(new RecycleEntry
            {
                Material = entry.Material,
                Pieces = entry.Pieces,
                PointsPerPiece = entry.PointsPerPiece
            });
        }

        return transaction;
    }

    private void StartDispenseFeedback(VendingItem product)
    {
        _isDispensing = true;
        imgDispense.Source = ImageLoader.LoadProductImage(product.ImagePath);
        imgDispense.Visibility = Visibility.Visible;
        imgDispense.Opacity = 1.0;

        SetDispenseStatus("DISPENSING...", Brushes.Goldenrod);

        // --- Randomized Dispense Animation ---
        Random rand = new Random();
        double targetX = rand.Next(-50, 50);
        double targetAngle = rand.Next(-15, 15);

        // Initial state: Center, but we animate Y from above
        imgDispenseTranslate.X = 0;
        imgDispenseTranslate.Y = 0; 
        imgDispenseRotate.Angle = 0;

        Storyboard sb = new Storyboard();

        // 1. Drop and Bounce (Y) - Animate FROM -300 TO 0
        DoubleAnimationUsingKeyFrames dropAnim = new DoubleAnimationUsingKeyFrames();
        dropAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(-300, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        dropAnim.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8)), new BounceEase { Bounces = 3, Bounciness = 2 }));
        Storyboard.SetTarget(dropAnim, imgDispenseTranslate);
        Storyboard.SetTargetProperty(dropAnim, new PropertyPath(TranslateTransform.YProperty));
        sb.Children.Add(dropAnim);

        // 2. Horizontal Shift (X)
        DoubleAnimation xAnim = new DoubleAnimation(0, targetX, TimeSpan.FromSeconds(0.8))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(xAnim, imgDispenseTranslate);
        Storyboard.SetTargetProperty(xAnim, new PropertyPath(TranslateTransform.XProperty));
        sb.Children.Add(xAnim);

        // 3. Random Rotation (Angle)
        DoubleAnimation rotAnim = new DoubleAnimation(0, targetAngle, TimeSpan.FromSeconds(0.8))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(rotAnim, imgDispenseRotate);
        Storyboard.SetTargetProperty(rotAnim, new PropertyPath(RotateTransform.AngleProperty));
        sb.Children.Add(rotAnim);

        sb.Begin(); // Start with default name scope
        // -------------------------------------

        if (_dispenseTimer != null)
        {
            _dispenseTimer.Stop();
        }

        _dispenseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3.0)
        };
        _dispenseTimer.Tick += (_, _) =>
        {
            _dispenseTimer?.Stop();
            _dispenseTimer = null;
            _isDispensing = false;
            SetDispenseStatus($"TAKE YOUR ITEM\n{product.DispenseMessage}", Brushes.MediumSeaGreen);
        };
        _dispenseTimer.Start();
    }

    private void StartBlink(int slotId)
    {
        _blinkSlotId = slotId;
        _blinkCount = 0;

        _blinkTimer?.Stop();
        _blinkTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _blinkTimer.Tick += BlinkTimer_Tick;
        _blinkTimer.Start();
    }

    private void BlinkTimer_Tick(object? sender, EventArgs e)
    {
        if (!_slots.TryGetValue(_blinkSlotId, out var slot))
        {
            _blinkTimer?.Stop();
            return;
        }

        _blinkCount++;
        if (_blinkCount > 6)
        {
            _blinkTimer?.Stop();
            _blinkTimer = null;

            var product = DataStore.Products.FirstOrDefault(p => p.Id == _blinkSlotId);
            slot.Panel.Background = product is { Stock: > 0 } ? SlotDefault : SlotEmpty;
            return;
        }

        slot.Panel.Background = _blinkCount % 2 == 1 ? SlotAlert : SlotDefault;
    }

    private void SetDispenseStatus(string text, Brush color)
    {
        // Sync with the virtual LCD display
        if (lblLcdDisplay != null)
        {
            lblLcdDisplay.Text = text.ToUpper().Replace("\n", " ");
        }

        // Sync with the physical Arduino LCD display
        _arduino?.SendMessage(text.ToUpper().Replace("\n", " "));
    }

    private void BtnHelp_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "HOW TO USE:\n\n" +
            "1. Insert bills to add money.\n" +
            "2. Press SELECT below a product slot.\n" +
            "3. Use RECYCLE FOR CREDIT if needed.\n" +
            "4. Use EXAMINE to view item details.\n" +
            "5. Your remaining balance will be returned automatically when you click DONE.",
            "Help - ECO-MATIC",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        // Show receipt first if a transaction exists
        if (DataStore.LastTransaction != null)
        {
            var receipt = new ReceiptWindow(DataStore.LastTransaction)
            {
                Owner = this
            };
            receipt.ShowDialog();
            DataStore.LastTransaction = null;
        }

        // Automatically return change
        if (_insertedMoney > 0)
        {
            decimal returned = _insertedMoney;
            _insertedMoney = 0;
            UpdateMoneyDisplay();
            
            MessageBox.Show(this,
                $"P{returned:F2} change returned. Thank you for using Eco-Matic!",
                "Change Returned",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        Close();
    }

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "Eco-Matic Vending Machine\nVersion 1.0\n\nCopyright 2026 Seanix",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
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
