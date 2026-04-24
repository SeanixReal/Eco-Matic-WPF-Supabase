using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Eco_Matic.Data;
using QRCoder;

namespace Eco_Matic;

public partial class QrPaymentWindow : Window
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1.2);

    private readonly int _machineId;
    private readonly DispatcherTimer _pollTimer;
    private QrPaymentIntent? _intent;
    private bool _isPolling;
    private bool _isSettingAmount;

    public decimal PaidAmount { get; private set; }

    public QrPaymentWindow(decimal defaultAmount, int machineId)
    {
        InitializeComponent();
        decimal requestedAmount = defaultAmount > 0 ? defaultAmount : 50m;
        _machineId = machineId;

        SetAmountText(requestedAmount);

        _pollTimer = new DispatcherTimer
        {
            Interval = PollInterval
        };
        _pollTimer.Tick += PollTimer_Tick;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await CreateOrRefreshIntentAsync();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _pollTimer.Stop();
    }

    private async void BtnUpdateQr_Click(object sender, RoutedEventArgs e)
    {
        await CreateOrRefreshIntentAsync();
    }

    private void TxtAmount_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isSettingAmount || btnUpdateQr == null)
        {
            return;
        }

        _pollTimer.Stop();
        _intent = null;
        imgQrCode.Source = null;
        txtReference.Text = "REF: UPDATE QR";
        txtStatus.Text = "Amount changed. Press UPDATE QR.";
        txtStatus.Foreground = CreateBrush(106, 120, 145);
        btnUpdateQr.IsEnabled = true;
    }

    private async Task CreateOrRefreshIntentAsync()
    {
        if (!TryReadAmount(out decimal amount))
        {
            txtStatus.Text = "Enter a valid amount.";
            txtStatus.Foreground = Brushes.IndianRed;
            return;
        }

        _pollTimer.Stop();
        btnUpdateQr.IsEnabled = false;
        txtStatus.Text = "Generating secure payment QR...";
        txtStatus.Foreground = CreateBrush(106, 120, 145);
        txtReference.Text = string.Empty;
        imgQrCode.Source = null;

        try
        {
            SetAmountText(amount);
            _intent = await QrPaymentService.Instance.CreateIntentAsync(_machineId, amount);
            txtReference.Text = $"REF: {_intent.Reference}";
            txtStatus.Text = "Waiting for QR scan...";
            txtStatus.Foreground = CreateBrush(106, 120, 145);
            RefreshQrCode(_intent.ConfirmUrl);
            _pollTimer.Start();
        }
        catch (Exception ex)
        {
            btnUpdateQr.IsEnabled = true;
            txtStatus.Text = "QR payment service is unavailable.";
            txtStatus.Foreground = Brushes.IndianRed;
            MessageBox.Show(this,
                $"Eco-Matic could not create a QR payment request.\n\n{ex.Message}",
                "QR Payment",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private async void PollTimer_Tick(object? sender, EventArgs e)
    {
        if (_intent == null || _isPolling)
        {
            return;
        }

        _isPolling = true;
        try
        {
            QrPaymentStatus status = await QrPaymentService.Instance.GetStatusAsync(_intent.Reference, _intent.Token);
            if (string.Equals(status.Status, "paid", StringComparison.OrdinalIgnoreCase))
            {
                _pollTimer.Stop();
                PaidAmount = status.Amount;
                txtStatus.Text = "Payment confirmed!";
                txtStatus.Foreground = Brushes.MediumSeaGreen;
                DialogResult = true;
            }
        }
        catch
        {
            txtStatus.Text = "Still waiting for payment confirmation...";
            txtStatus.Foreground = CreateBrush(106, 120, 145);
        }
        finally
        {
            _isPolling = false;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void WindowFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private bool TryReadAmount(out decimal amount)
    {
        string raw = txtAmount.Text.Trim()
            .Replace("PHP", "", StringComparison.OrdinalIgnoreCase)
            .Replace("P", "", StringComparison.OrdinalIgnoreCase);

        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) &&
               amount > 0;
    }

    private void SetAmountText(decimal amount)
    {
        _isSettingAmount = true;
        txtAmount.Text = amount.ToString("0.00", CultureInfo.InvariantCulture);
        _isSettingAmount = false;
    }

    private void RefreshQrCode(string payload)
    {
        byte[] qrBytes = PngByteQRCodeHelper.GetQRCode(payload, QRCodeGenerator.ECCLevel.Q, 16);

        using var stream = new MemoryStream(qrBytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        imgQrCode.Source = image;
    }

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
