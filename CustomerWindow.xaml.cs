using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Threading;
using Eco_Matic.Data;
using Eco_Matic.Utilities;

namespace Eco_Matic;

public partial class CustomerWindow : Window
{
    private static readonly TimeSpan DefaultLiveInventoryRefreshInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryLiveInventoryRefreshInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CooldownLiveInventoryRefreshInterval = TimeSpan.FromSeconds(18);
    private static readonly Random AnimationRandom = new();

    private readonly List<RecycleEntry> _recycleEntries = new();
    private readonly Dictionary<int, SlotControls> _slots = new();
    private readonly List<Product> _products = new();

    private decimal _insertedMoney;
    private decimal _totalMoneyInserted;
    private decimal _totalChangeReturned;
    private int _pendingPoints;
    private int _availableEcoCredits;
    private string? _linkedRfid;
    private string? _linkedCustomerEmail;
    private bool _isDispensing;
    private bool _payWithPoints;
    private readonly Data.ArduinoService? _arduino;
    private readonly int _machineId;
    private readonly string _machineDisplayName;
    private readonly string _machineAddress;
    private DispatcherTimer? _dispenseTimer;
    private DispatcherTimer? _blinkTimer;
    private int _blinkSlotId;
    private int _blinkCount;
    private Transaction _activeSession = new();
    private bool _isRefreshingInventory;
    private CancellationTokenSource? _liveInventoryRefreshCts;
    private int _consecutiveRefreshFailures;
    private int _pendingBackendWrites;
    private bool _allowWindowClose;
    private bool _hardwareActivated;

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
        public int CatalogItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public CustomerWindow(
        int machineId,
        string machineDisplayName,
        string machineAddress,
        IEnumerable<Product> initialProducts,
        Data.ArduinoService? arduino = null)
    {
        InitializeComponent();
        _machineId = machineId;
        _machineDisplayName = machineDisplayName;
        _machineAddress = machineAddress;
        _arduino = arduino;
        Loaded += CustomerWindow_Loaded;
        StartNewSession();
        InitializeSlots();
        InitializeSelectors();
        ReplaceProducts(initialProducts);
        RefreshProducts();
        UpdateMachineHeader();
        UpdateMoneyDisplay();
        UpdateDoneButtonState();
        SetDispenseStatus("INSERT MONEY TO START", StatusIdle);
    }

    private async void CustomerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= CustomerWindow_Loaded;
        ActivateHardwareSession();
        AudioService.PlayBackgroundMusic("Assets/Audio/lobby.mp3");
        await Task.Delay(500); // Wait for audio engine to wake up to prevent TTS cutoff
        AudioService.SpeakAsync("Welcome to Eco-Matic. Please insert money or tap your R F I D card to begin.");
        await LoadRecycleCatalogAsync();
        InitializeLiveInventoryRefresh();
    }

    private void ActivateHardwareSession()
    {
        if (_hardwareActivated)
        {
            return;
        }

        _hardwareActivated = true;
        _arduino?.SendCustomerSessionActive();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        AudioService.StopBackgroundMusic();
        AudioService.SpeakAsync("Thank you for using Eco-Matic. Have a nice day!");
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

        if (_liveInventoryRefreshCts != null)
        {
            _liveInventoryRefreshCts.Cancel();
            _liveInventoryRefreshCts.Dispose();
            _liveInventoryRefreshCts = null;
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
        cboRecycleType.IsEnabled = false;
        txtRecycleQty.Text = "1";
        RefreshExamineOptions();
    }

    private async Task LoadRecycleCatalogAsync()
    {
        bool loaded = await Task.Run(DataStore.RefreshRecyclableCatalog);

        cboRecycleType.ItemsSource = null;
        cboRecycleType.Items.Clear();

        if (!loaded || DataStore.RecyclableItems.Count == 0)
        {
            cboRecycleType.IsEnabled = false;
            return;
        }

        cboRecycleType.ItemsSource = DataStore.RecyclableItems;
        cboRecycleType.SelectedIndex = 0;
        cboRecycleType.IsEnabled = true;
    }

    private void RefreshProducts()
    {
        foreach (var slotPair in _slots)
        {
            int slotId = slotPair.Key;
            SlotControls slot = slotPair.Value;

            var product = _products.FirstOrDefault(p => p.Id == slotId);
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

    private void UpdateMachineHeader()
    {
        string machineName = string.IsNullOrWhiteSpace(_machineDisplayName)
            ? $"Machine {_machineId}"
            : _machineDisplayName;

        txtMachineHeader.Text = string.IsNullOrWhiteSpace(_machineAddress)
            ? machineName
            : $"{machineName}  |  {_machineAddress}";
    }

    private void InitializeLiveInventoryRefresh()
    {
        if (OfflineSyncCoordinator.Instance.CurrentSource != SessionDataSource.Supabase)
        {
            return;
        }

        _liveInventoryRefreshCts?.Cancel();
        _liveInventoryRefreshCts?.Dispose();
        _liveInventoryRefreshCts = new CancellationTokenSource();
        _ = RunLiveInventoryRefreshLoopAsync(_liveInventoryRefreshCts.Token);
    }

    private async Task RunLiveInventoryRefreshLoopAsync(CancellationToken cancellationToken)
    {
        TimeSpan delay = DefaultLiveInventoryRefreshInterval;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            if (!IsLoaded || !IsVisible || _isDispensing || Volatile.Read(ref _pendingBackendWrites) > 0)
            {
                continue;
            }

            bool refreshed = await RefreshInventoryFromSourceAsync();
            if (refreshed)
            {
                _consecutiveRefreshFailures = 0;
                delay = DefaultLiveInventoryRefreshInterval;
            }
            else
            {
                _consecutiveRefreshFailures++;
                delay = _consecutiveRefreshFailures >= 3
                    ? CooldownLiveInventoryRefreshInterval
                    : RetryLiveInventoryRefreshInterval;
            }
        }
    }

    private async Task<bool> RefreshInventoryFromSourceAsync()
    {
        if (_isRefreshingInventory ||
            _isDispensing ||
            OfflineSyncCoordinator.Instance.CurrentSource != SessionDataSource.Supabase)
        {
            return false;
        }

        _isRefreshingInventory = true;

        try
        {
            var refreshedProducts = await Task.Run(() =>
            {
                return DataStore.TryGetProductsForMachine(_machineId, out var products)
                    ? products
                    : new List<Product>();
            });

            if (refreshedProducts.Count == 0)
            {
                return false;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ReplaceProducts(refreshedProducts);
                RefreshProducts();
                UpdateAllButtonStates();
            });
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _isRefreshingInventory = false;
        }
    }

    private void UpdateAllButtonStates()
    {
        foreach (var slotPair in _slots)
        {
            var product = _products.FirstOrDefault(p => p.Id == slotPair.Key);
            if (product is { Stock: > 0 })
            {
                UpdateButtonBuyability(slotPair.Value, product);
            }
        }
    }

    private void UpdateButtonBuyability(SlotControls slot, VendingItem product)
    {
        if (_payWithPoints)
        {
            int pointsCost = GetPointCost(product);
            if (GetTotalEcoPoints() >= pointsCost)
            {
                slot.SelectButton.Content = "POINT PAY";
                slot.SelectButton.Background = CreateBrush(255, 206, 74);
                slot.SelectButton.Foreground = CreateBrush(29, 35, 63);
                slot.SelectButton.BorderBrush = CreateBrush(255, 206, 74);
            }
            else
            {
                slot.SelectButton.Content = "NEED POINTS";
                slot.SelectButton.Background = ButtonDim;
                slot.SelectButton.Foreground = CreateBrush(48, 65, 92);
                slot.SelectButton.BorderBrush = CreateBrush(201, 216, 239);
            }

            return;
        }

        slot.SelectButton.Content = "SELECT";
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
        int? selectedCatalogItemId = (cboExamineItem.SelectedItem as VendingItemOption)?.CatalogItemId;

        cboExamineItem.Items.Clear();
        foreach (var product in _products
                     .GroupBy(p => p.CatalogItemId > 0 ? p.CatalogItemId : p.Id)
                     .Select(group => group
                         .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(p => p.Id)
                         .First())
                     .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            cboExamineItem.Items.Add(new VendingItemOption
            {
                CatalogItemId = product.CatalogItemId > 0 ? product.CatalogItemId : product.Id,
                Name = product.Name
            });
        }

        if (selectedCatalogItemId.HasValue)
        {
            var same = cboExamineItem.Items
                .OfType<VendingItemOption>()
                .FirstOrDefault(x => x.CatalogItemId == selectedCatalogItemId.Value);
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
        lblCashAmount.Text = $"P {_insertedMoney:F2}";
        int totalEcoPoints = GetTotalEcoPoints();
        if (totalEcoPoints <= 0)
        {
            _payWithPoints = false;
        }

        if (!string.IsNullOrWhiteSpace(_linkedRfid))
        {
            lblPointsAmount.Text = _pendingPoints > 0
                ? $"{_availableEcoCredits}+{_pendingPoints}"
                : _availableEcoCredits.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            lblPointsAmount.Text = _pendingPoints.ToString(CultureInfo.InvariantCulture);
        }

        if (btnPayWithPoints != null)
        {
            btnPayWithPoints.IsEnabled = totalEcoPoints > 0;
            btnPayWithPoints.Content = _payWithPoints
                ? "PAYING WITH POINTS"
                : totalEcoPoints > 0
                    ? "PAY WITH POINTS"
                    : "NO ECO POINTS";

            btnPayWithPoints.Background = _payWithPoints
                ? CreateBrush(255, 206, 74)
                : CreateBrush(47, 166, 106);
            btnPayWithPoints.Foreground = _payWithPoints
                ? CreateBrush(29, 35, 63)
                : Brushes.White;
        }
    }

    public void MarkPendingPointsSaved(int savedPoints)
    {
        int pointsToClear = Math.Clamp(savedPoints, 0, _pendingPoints);
        _pendingPoints -= pointsToClear;
        DataStore.PendingPoints = _pendingPoints;
        UpdateMoneyDisplay();
        SetDispenseStatus(pointsToClear > 0 ? "POINTS SAVED TO RFID" : "NO POINTS SAVED", pointsToClear > 0 ? Brushes.MediumSeaGreen : StatusIdle);
        UpdateDoneButtonState();
    }

    public void SetLinkedRfidCustomer(string rfid, string email, int ecoCredits)
    {
        _linkedRfid = rfid;
        _linkedCustomerEmail = email;
        _availableEcoCredits = Math.Max(0, ecoCredits);
        _payWithPoints = false;
        UpdateMoneyDisplay();

        SetDispenseStatus($"{GetLinkedCustomerLabel()} {_availableEcoCredits} PTS", Brushes.MediumSeaGreen);
    }

    private string GetLinkedCustomerLabel()
    {
        if (string.IsNullOrWhiteSpace(_linkedCustomerEmail))
        {
            return "RFID READY";
        }

        string name = _linkedCustomerEmail.Split('@')[0].Trim();
        return string.IsNullOrWhiteSpace(name) ? "RFID READY" : name.ToUpperInvariant();
    }

    private void StartNewSession()
    {
        DateTime now = DateTime.Now;
        _totalMoneyInserted = 0m;
        _totalChangeReturned = 0m;
        _pendingPoints = 0;
        _availableEcoCredits = 0;
        _linkedRfid = null;
        _linkedCustomerEmail = null;
        _payWithPoints = false;
        DataStore.PendingPoints = 0;
        _activeSession = new Transaction
        {
            Id = DataStore.AllocateTransactionId(),
            ReceiptNumber = $"RCPT-{now:yyyyMMddHHmmssfff}",
            MachineId = _machineId,
            MachineDisplayName = _machineDisplayName,
            MachineAddress = _machineAddress,
            SessionStartedAt = now,
            SessionEndedAt = now,
            Date = now,
            Source = DataStore.IsOffline ? "offline" : "online"
        };
    }

    private bool HasSessionActivity()
    {
        return _activeSession.Items.Count > 0 ||
               _recycleEntries.Count > 0 ||
               _totalMoneyInserted > 0m ||
               _totalChangeReturned > 0m ||
               _insertedMoney > 0m;
    }

    private void UpdateDoneButtonState()
    {
        if (HasSessionActivity())
        {
            btnBack.Content = "DONE & RECEIPT";
            btnBack.Background = new SolidColorBrush(Color.FromRgb(47, 166, 106));
            btnBack.BorderBrush = new SolidColorBrush(Color.FromRgb(47, 166, 106));
        }
        else
        {
            btnBack.Content = "DONE";
            btnBack.Background = new SolidColorBrush(Color.FromRgb(46, 119, 230));
            btnBack.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 119, 230));
        }
    }

    private void QueueBackgroundStoreAction(Action action)
    {
        Interlocked.Increment(ref _pendingBackendWrites);

        _ = Task.Run(() =>
        {
            try
            {
                action();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Decrement(ref _pendingBackendWrites);
            }
        });
    }

    private void ReplaceProducts(IEnumerable<Product> products)
    {
        _products.Clear();
        _products.AddRange(products.Select(CloneProduct));
    }

    private static Product CloneProduct(Product source)
    {
        Product clone = Product.Create(
            source.Type,
            source.Id,
            source.Name,
            source.Price,
            source.Stock,
            source.FlavorText,
            source is IHasCalories caloriesItem ? caloriesItem.Calories : 0,
            source is IHasVolume volumeItem ? volumeItem.VolumeMl : 0,
            source.ImagePath,
            source.DispenseMessage,
            source.ExamineMessage);

        clone.DbInventoryId = source.DbInventoryId;
        clone.CatalogItemId = source.CatalogItemId;
        return clone;
    }

    private static Product CreateInventorySaveSnapshot(int inventoryId, int stockLevel)
    {
        return new MiscItem
        {
            DbInventoryId = inventoryId,
            Stock = stockLevel
        };
    }

    private void AddProductToActiveSession(VendingItem product)
    {
        string slotId = product.Id.ToString(CultureInfo.InvariantCulture);
        TransactionItem? existingLine = _activeSession.Items.FirstOrDefault(item =>
            item.ProductId == product.DbInventoryId &&
            string.Equals(item.SlotId, slotId, StringComparison.Ordinal));

        if (existingLine == null)
        {
            _activeSession.Items.Add(new TransactionItem
            {
                ProductId = product.DbInventoryId,
                SlotId = slotId,
                ProductName = product.Name,
                Quantity = 1,
                UnitPrice = product.Price
            });
        }
        else
        {
            existingLine.Quantity++;
        }

        _activeSession.TotalAmount = _activeSession.Items.Sum(item => item.LineTotal);
        _activeSession.AmountPaid = _totalMoneyInserted;
        _activeSession.Change = _totalChangeReturned + _insertedMoney;
    }

    private Transaction FinalizeActiveSession()
    {
        _activeSession.RecycledItems = _recycleEntries
            .Select(entry => new RecycleEntry
            {
                RecyclableItemId = entry.RecyclableItemId,
                DisplayName = entry.DisplayName,
                MaterialType = entry.MaterialType,
                UnitLabel = entry.UnitLabel,
                Pieces = entry.Pieces,
                PointsPerUnit = entry.PointsPerUnit,
                Description = entry.Description
            })
            .ToList();

        _activeSession.TotalAmount = _activeSession.Items.Sum(item => item.LineTotal);
        _activeSession.AmountPaid = _totalMoneyInserted;
        _activeSession.Change = _totalChangeReturned + _insertedMoney;
        _activeSession.SessionEndedAt = DateTime.Now;
        _activeSession.Date = _activeSession.SessionEndedAt;
        _activeSession.Source = DataStore.IsOffline ? "offline" : "online";
        return _activeSession;
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
        _totalMoneyInserted += amount;
        AudioService.PlaySfx("Assets/Audio/coins.mp3");
        _arduino?.SendMessage("CASH INSERTED");
        UpdateMoneyDisplay();
        UpdateAllButtonStates();
        UpdateDoneButtonState();
    }

    private void BtnQrPay_Click(object sender, RoutedEventArgs e)
    {
        var qrPayment = new QrPaymentWindow(GetSuggestedQrPaymentAmount(), _machineId)
        {
            Owner = this
        };

        if (qrPayment.ShowDialog() != true)
        {
            return;
        }

        _insertedMoney += qrPayment.PaidAmount;
        _totalMoneyInserted += qrPayment.PaidAmount;
        AudioService.PlaySfx("Assets/Audio/success.mp3");

        SetDispenseStatus("QR PAYMENT OK", Brushes.MediumSeaGreen);
        UpdateMoneyDisplay();
        UpdateAllButtonStates();
        UpdateDoneButtonState();
    }

    private void BtnPayWithPoints_Click(object sender, RoutedEventArgs e)
    {
        if (GetTotalEcoPoints() <= 0)
        {
            SetDispenseStatus("NO ECO POINTS", SoldOutRed);
            return;
        }

        _payWithPoints = !_payWithPoints;
        SetDispenseStatus(_payWithPoints ? "POINT PAY READY" : "POINT PAY OFF", _payWithPoints ? Brushes.Goldenrod : StatusIdle);
        UpdateMoneyDisplay();
        UpdateAllButtonStates();
    }

    private int GetTotalEcoPoints()
    {
        return Math.Max(0, _availableEcoCredits) + Math.Max(0, _pendingPoints);
    }

    private async Task<bool> UpdateLinkedCustomerCreditsAsync(int newBalance)
    {
        string? rfid = _linkedRfid;
        if (string.IsNullOrWhiteSpace(rfid))
        {
            return false;
        }

        return await Task.Run(() =>
        {
            var store = new Data.SupabaseStore();
            return store.UpdateCustomerCredits(rfid, Math.Max(0, newBalance));
        });
    }

    private decimal GetSuggestedQrPaymentAmount()
    {
        decimal cheapestAvailablePrice = _products
            .Where(product => product.Stock > 0)
            .Select(product => product.Price)
            .DefaultIfEmpty(50m)
            .Min();

        decimal remainingForCheapestItem = cheapestAvailablePrice - _insertedMoney;
        return remainingForCheapestItem > 0 ? remainingForCheapestItem : 20m;
    }

    private async void BtnCoinReturn_Click(object sender, RoutedEventArgs e)
    {
        await ReturnRemainingBalanceAsync(true);
    }

    private async Task<bool> ReturnRemainingBalanceAsync(bool showMessage)
    {
        if (_insertedMoney <= 0)
        {
            return true;
        }

        decimal returned = _insertedMoney;
        _totalChangeReturned += returned;
        _insertedMoney = 0;

        AudioService.PlaySfx("Assets/Audio/coin_dispense.mp3");

        UpdateMoneyDisplay();
        SetDispenseStatus("CHANGE RETURNED", StatusIdle);
        UpdateAllButtonStates();
        UpdateDoneButtonState();

        if (showMessage)
        {
            MessageBox.Show(this,
                $"P{returned:F2} returned. Please collect your money.",
                "Coin Return",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        return true;
    }

    private void BtnRecycle_Click(object sender, RoutedEventArgs e)
    {
        if (cboRecycleType.SelectedItem is not RecyclableItemDefinition recyclableItem)
        {
            return;
        }

        if (!int.TryParse(txtRecycleQty.Text, out int pieces) || pieces <= 0)
        {
            SetDispenseStatus("RECYCLE QTY ERROR", SoldOutRed);
            MessageBox.Show(this,
                "Enter a valid number of items (pieces) greater than zero.",
                "Recycle Credit",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        int points = recyclableItem.PointsPerUnit * pieces;
        _pendingPoints += points;
        DataStore.PendingPoints = _pendingPoints;

        var existing = _recycleEntries.FirstOrDefault(x => x.RecyclableItemId == recyclableItem.Id);
        if (existing == null)
        {
            _recycleEntries.Add(new RecycleEntry
            {
                RecyclableItemId = recyclableItem.Id,
                DisplayName = recyclableItem.DisplayName,
                MaterialType = recyclableItem.MaterialType,
                UnitLabel = recyclableItem.UnitLabel,
                Pieces = pieces,
                PointsPerUnit = recyclableItem.PointsPerUnit,
                Description = recyclableItem.Description
            });
        }
        else
        {
            existing.Pieces += pieces;
        }

        string recycleLogDetails = $"{pieces} {recyclableItem.UnitLabel}(s) {recyclableItem.DisplayName}";
        QueueBackgroundStoreAction(() => DataStore.LogEvent(_machineId, "RECYCLE", recycleLogDetails, points));
        txtRecycleQty.Text = "1";
        SetDispenseStatus($"+{points} POINTS ADDED", Brushes.MediumSeaGreen);
        UpdateMoneyDisplay();
        UpdateAllButtonStates();
        UpdateDoneButtonState();
    }

    private void BtnExamine_Click(object sender, RoutedEventArgs e)
    {
        if (cboExamineItem.SelectedItem is not VendingItemOption option)
        {
            return;
        }

        int targetCatalogItemId = option.CatalogItemId;
        List<Product> matchingProducts = _products
            .Where(product => (product.CatalogItemId > 0 ? product.CatalogItemId : product.Id) == targetCatalogItemId)
            .OrderBy(product => product.Id)
            .ToList();

        if (matchingProducts.Count == 0)
        {
            RefreshProducts();
            return;
        }

        var detailsWindow = new ItemDetailsWindow(matchingProducts)
        {
            Owner = this
        };
        detailsWindow.ShowDialog();
    }

    private async void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isDispensing)
        {
            return;
        }

        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out int slotId))
        {
            return;
        }

        if (OfflineSyncCoordinator.Instance.CurrentSource == SessionDataSource.Supabase)
        {
            bool refreshed = await RefreshInventoryFromSourceAsync();
            if (!refreshed)
            {
                SetDispenseStatus("LIVE STOCK DELAYED", Brushes.Khaki);
            }
        }

        var product = _products.FirstOrDefault(p => p.Id == slotId);
        if (product == null)
        {
            return;
        }

        if (product.Stock <= 0)
        {
            SetDispenseStatus("Item is sold out.", Brushes.IndianRed);
            return;
        }

        if (_payWithPoints)
        {
            await PurchaseWithPointsAsync(product, slotId);
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
        AddProductToActiveSession(product);

        int inventoryId = product.DbInventoryId;
        int updatedStock = product.Stock;
        string logDetails = $"Item: {product.Name} | Quantity: 1 | Price: ₱{product.Price:0.00} | Total: ₱{product.Price:0.00}";
        QueueBackgroundStoreAction(() =>
        {
            DataStore.SaveInventory(_machineId, CreateInventorySaveSnapshot(inventoryId, updatedStock));
            DataStore.LogEvent(_machineId, "PURCHASE", logDetails, product.Price);
            DataStore.RecordSale(_machineId, inventoryId, product.Price);
        });

        ImageSource? dispenseSource = _slots.TryGetValue(slotId, out SlotControls slotControls)
            ? slotControls.VendingItemImage.Source
            : null;

        StartDispenseFeedback(product, dispenseSource);
        UpdateMoneyDisplay();
        RefreshProducts();
        UpdateDoneButtonState();
    }

    private async Task PurchaseWithPointsAsync(Product product, int slotId)
    {
        int pointsCost = GetPointCost(product);
        if (GetTotalEcoPoints() < pointsCost)
        {
            StartBlink(slotId);
            SetDispenseStatus($"NEED {pointsCost} POINTS", SoldOutRed);
            return;
        }

        int pendingPointsSpent = Math.Min(_pendingPoints, pointsCost);
        int savedPointsSpent = pointsCost - pendingPointsSpent;

        if (savedPointsSpent > 0 && string.IsNullOrWhiteSpace(_linkedRfid))
        {
            StartBlink(slotId);
            SetDispenseStatus($"NEED {pointsCost} POINTS", SoldOutRed);
            return;
        }

        if (savedPointsSpent > 0)
        {
            bool updated = await UpdateLinkedCustomerCreditsAsync(_availableEcoCredits - savedPointsSpent);
            if (!updated)
            {
                SetDispenseStatus("POINT PAY ERROR", SoldOutRed);
                MessageBox.Show(this,
                    "Eco-Matic could not deduct saved RFID points. You can still spend unsaved session points.",
                    "Point Payment",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        _pendingPoints -= pendingPointsSpent;
        DataStore.PendingPoints = _pendingPoints;
        _availableEcoCredits -= savedPointsSpent;
        _totalMoneyInserted += product.Price;

        product.Stock--;
        AddProductToActiveSession(product);

        int inventoryId = product.DbInventoryId;
        int updatedStock = product.Stock;
        string pointSource = savedPointsSpent > 0
            ? $"session points: {pendingPointsSpent}, saved RFID points: {savedPointsSpent}"
            : $"session points: {pendingPointsSpent}";
        string logDetails = $"Item: {product.Name} | Quantity: 1 | Paid with {pointsCost} eco points ({pointSource}) | Total: ₱{product.Price:0.00}";
        QueueBackgroundStoreAction(() =>
        {
            DataStore.SaveInventory(_machineId, CreateInventorySaveSnapshot(inventoryId, updatedStock));
            DataStore.LogEvent(_machineId, "POINT_PURCHASE", logDetails, product.Price);
            DataStore.RecordSale(_machineId, inventoryId, product.Price);
        });

        ImageSource? dispenseSource = _slots.TryGetValue(slotId, out SlotControls slotControls)
            ? slotControls.VendingItemImage.Source
            : null;

        SetDispenseStatus($"{pointsCost} POINTS PAID", Brushes.MediumSeaGreen);
        _arduino?.SendMessage("POINT PAYMENT OK");
        StartDispenseFeedback(product, dispenseSource);
        UpdateMoneyDisplay();
        RefreshProducts();
        UpdateDoneButtonState();
    }

    private static int GetPointCost(VendingItem product)
    {
        return Math.Max(1, (int)Math.Ceiling(product.Price));
    }

    private void StartDispenseFeedback(VendingItem product, ImageSource? dispenseSource)
    {
        _isDispensing = true;
        imgDispense.Source = dispenseSource ?? ImageLoader.LoadProductImage(product.ImagePath);
        imgDispense.Visibility = Visibility.Visible;
        imgDispense.Opacity = 1.0;
        Panel.SetZIndex(imgDispense, 5);

        SetDispenseStatus("DISPENSING...", Brushes.Goldenrod);

        double trayWidth = pnlDispenseTray.ActualWidth > 0 ? pnlDispenseTray.ActualWidth : 280;
        double trayHeight = pnlDispenseTray.ActualHeight > 0 ? pnlDispenseTray.ActualHeight : 150;
        double maxHorizontalTravel = Math.Max(24, Math.Min(70, (trayWidth / 2) - 55));
        double targetX = (AnimationRandom.NextDouble() * 2 - 1) * maxHorizontalTravel;
        double startY = -Math.Max(120, trayHeight * 0.95);
        double settleY = Math.Min(18, trayHeight * 0.14);
        double landingAngle = (AnimationRandom.NextDouble() * 18) - 9;
        double settleAngle = landingAngle * 0.35;

        imgDispenseTranslate.X = 0;
        imgDispenseTranslate.Y = startY;
        imgDispenseRotate.Angle = -landingAngle * 0.45;
        imgDispenseTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        imgDispenseTranslate.BeginAnimation(TranslateTransform.YProperty, null);
        imgDispenseRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        imgDispense.BeginAnimation(UIElement.OpacityProperty, null);

        var dropAnim = new DoubleAnimationUsingKeyFrames();
        dropAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(startY, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        dropAnim.KeyFrames.Add(new EasingDoubleKeyFrame(settleY, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.82)), new BounceEase
        {
            Bounces = 2,
            Bounciness = 1.7,
            EasingMode = EasingMode.EaseOut
        }));
        imgDispenseTranslate.BeginAnimation(TranslateTransform.YProperty, dropAnim);

        var xAnim = new DoubleAnimationUsingKeyFrames();
        xAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        xAnim.KeyFrames.Add(new EasingDoubleKeyFrame(targetX * 0.82, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.54)), new CubicEase { EasingMode = EasingMode.EaseOut }));
        xAnim.KeyFrames.Add(new EasingDoubleKeyFrame(targetX, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.82)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        imgDispenseTranslate.BeginAnimation(TranslateTransform.XProperty, xAnim);

        var rotAnim = new DoubleAnimationUsingKeyFrames();
        rotAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(-landingAngle * 0.45, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        rotAnim.KeyFrames.Add(new EasingDoubleKeyFrame(landingAngle, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.46)), new SineEase { EasingMode = EasingMode.EaseOut }));
        rotAnim.KeyFrames.Add(new EasingDoubleKeyFrame(settleAngle, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.82)), new CubicEase { EasingMode = EasingMode.EaseOut }));
        imgDispenseRotate.BeginAnimation(RotateTransform.AngleProperty, rotAnim);

        var opacityAnim = new DoubleAnimationUsingKeyFrames();
        opacityAnim.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.18)), new QuadraticEase { EasingMode = EasingMode.EaseOut }));
        imgDispense.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

        _ = FinishDispenseSequenceAsync(product);
    }

    private async Task FinishDispenseSequenceAsync(VendingItem product)
    {
        AudioService.PlaySfx("Assets/Audio/coin_dispense.mp3");
        AudioService.SpeakAsync($"Dispensing {product.Name}");
        // Wait for the motor's open cycle (1.5 seconds)
        await Task.Delay(1500);
        
        _isDispensing = false;
        imgDispenseOpacityReset();
        SetDispenseStatus($"TAKE YOUR ITEM\n{product.DispenseMessage}", Brushes.MediumSeaGreen);
        AudioService.SpeakAsync($"Please take your {product.Name}. {product.DispenseMessage}");

        // Wait enough time for customer to read the message and motor to finish closing
        await Task.Delay(4000);

        if (!_isDispensing)
        {
            imgDispense.Visibility = Visibility.Hidden;
            SetDispenseStatus("ECO-MATIC READY\nCASH OR RECYCLE", Brushes.LightGray);
        }
    }

    private void imgDispenseOpacityReset()
    {
        imgDispense.Opacity = 1.0;
        imgDispenseTranslate.X = 0;
        imgDispenseTranslate.Y = 0;
        imgDispenseRotate.Angle = 0;
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

            var product = _products.FirstOrDefault(p => p.Id == _blinkSlotId);
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
            lblLcdDisplay.Foreground = color;
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
            "3. Use RECYCLE FOR ECO POINTS to earn points.\n" +
            "4. Toggle PAY WITH POINTS when you want points instead of cash.\n" +
            "5. Tap RFID only if you want to save unused points.\n" +
            "6. Your remaining cash balance is returned when you click DONE.",
            "Help - ECO-MATIC",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        Transaction? completedSession = null;
        ReceiptPrintResult? printResult = null;
        if (HasSessionActivity())
        {
            if (!await ReturnRemainingBalanceAsync(false))
            {
                return;
            }

            completedSession = FinalizeActiveSession();
            await Task.Run(() => DataStore.SaveCompletedReceipt(completedSession));
            _arduino?.SendMessage("PRINTING RECEIPT");
            AudioService.PlaySfx("Assets/Audio/coin_dispense.mp3");
            printResult = await Task.Run(() => ReceiptPrinterService.Instance.TryPrintReceipt(completedSession));
            _arduino?.SendMessage(printResult.Success ? "RECEIPT COMPLETE" : "RECEIPT FAILED");
            
            // Give the user a nice thank you message after the receipt flow
            await Task.Delay(1000);
            _arduino?.SendMessage("THANK YOU FOR\nUSING ECO-MATIC");
            AudioService.SpeakAsync("Thank you for using Eco-Matic. Please come again!");
            await Task.Delay(2000);

            var receipt = new ReceiptWindow(completedSession, printResult)
            {
                Owner = this
            };
            receipt.ShowDialog();
        }

        _allowWindowClose = true;
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

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowWindowClose && HasSessionActivity())
        {
            e.Cancel = true;
            MessageBox.Show(this,
                "This kiosk session already has activity. Use DONE & RECEIPT to finish the session instead of closing the window.",
                "Finish Current Session",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        base.OnClosing(e);
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
