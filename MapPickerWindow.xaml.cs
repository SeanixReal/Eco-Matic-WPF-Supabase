using System.Globalization;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Eco_Matic;

public partial class MapPickerWindow : Window
{
    private const double DefaultLatitude = 10.3157;
    private const double DefaultLongitude = 123.8854;

    public string SelectedAddress { get; private set; } = string.Empty;
    public double? SelectedLatitude { get; private set; }
    public double? SelectedLongitude { get; private set; }

    public MapPickerWindow(string? currentAddress = null, double? currentLatitude = null, double? currentLongitude = null)
    {
        InitializeComponent();
        SelectedAddress = currentAddress?.Trim() ?? string.Empty;
        SelectedLatitude = currentLatitude;
        SelectedLongitude = currentLongitude;
        txtResolvedAddress.Text = SelectedAddress;
        UpdateCoordinateText();
        Loaded += MapPickerWindow_Loaded;
    }

    private async void MapPickerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MapPickerWindow_Loaded;

        try
        {
            await mapView.EnsureCoreWebView2Async();
            mapView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            mapView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            mapView.NavigateToString(BuildMapHtml());
        }
        catch (Exception ex)
        {
            txtMapStatus.Text = $"Map picker could not be loaded. You can still type the address manually. {ex.Message}";
        }
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(e.WebMessageAsJson);
            JsonElement root = document.RootElement;
            if (!root.TryGetProperty("type", out JsonElement typeElement) ||
                typeElement.GetString() != "picked")
            {
                return;
            }

            double latitude = root.GetProperty("lat").GetDouble();
            double longitude = root.GetProperty("lon").GetDouble();

            SelectedLatitude = latitude;
            SelectedLongitude = longitude;
            UpdateCoordinateText();
            txtMapStatus.Text = "Resolving address from the selected point...";

            Data.MapLocationResult? result = await Data.MapLocationService.Instance.ReverseGeocodeAsync(latitude, longitude);
            if (result != null)
            {
                SelectedAddress = result.Address;
                txtResolvedAddress.Text = result.Address;
                txtMapStatus.Text = "Address loaded from the selected map point. You can still edit it before saving.";
            }
            else
            {
                txtMapStatus.Text = "Coordinates were captured, but no address was returned. You can type the address manually.";
            }
        }
        catch (Exception ex)
        {
            txtMapStatus.Text = $"Coordinates were captured, but address lookup failed. {ex.Message}";
        }
    }

    private void UpdateCoordinateText()
    {
        if (SelectedLatitude.HasValue && SelectedLongitude.HasValue)
        {
            txtCoordinates.Text =
                $"Lat: {SelectedLatitude.Value.ToString("F6", CultureInfo.InvariantCulture)}{Environment.NewLine}" +
                $"Lng: {SelectedLongitude.Value.ToString("F6", CultureInfo.InvariantCulture)}";
        }
        else
        {
            txtCoordinates.Text = "No location selected yet.";
        }
    }

    private void BtnUseLocation_Click(object sender, RoutedEventArgs e)
    {
        if (!SelectedLatitude.HasValue || !SelectedLongitude.HasValue)
        {
            MessageBox.Show(this,
                "Please click a point on the map first.",
                "Map Location",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        SelectedAddress = txtResolvedAddress.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private string BuildMapHtml()
    {
        double latitude = SelectedLatitude ?? DefaultLatitude;
        double longitude = SelectedLongitude ?? DefaultLongitude;

        return $$"""
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" crossorigin="" />
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js" crossorigin=""></script>
  <style>
    html, body, #map { height: 100%; margin: 0; font-family: Arial, sans-serif; }
    .hint {
      position: absolute;
      top: 12px;
      left: 12px;
      z-index: 500;
      background: rgba(255,255,255,0.95);
      border-radius: 8px;
      padding: 10px 12px;
      box-shadow: 0 8px 20px rgba(15, 23, 42, 0.12);
      font-size: 13px;
    }
  </style>
</head>
<body>
  <div class="hint">Click a point on the map to place the vending machine.</div>
  <div id="map"></div>
  <script>
        const initialLat = {{latitude.ToString(CultureInfo.InvariantCulture)}};
        const initialLon = {{longitude.ToString(CultureInfo.InvariantCulture)}};
        const map = L.map('map').setView([initialLat, initialLon], 13);

        const primaryTileUrl = 'https://{s}.tile.openstreetmap.fr/osmfr/{z}/{x}/{y}.png';
        const fallbackTileUrl = 'https://{s}.tile.openstreetmap.de/{z}/{x}/{y}.png';

        let usedFallback = false;
        const primaryLayer = L.tileLayer(primaryTileUrl, {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        const fallbackLayer = L.tileLayer(fallbackTileUrl, {
            maxZoom: 19,
            attribution: '&copy; OpenStreetMap contributors'
        });

        primaryLayer.on('tileerror', function() {
            if (!usedFallback) {
                usedFallback = true;
                map.removeLayer(primaryLayer);
                fallbackLayer.addTo(map);
            }
        });

        let marker = L.marker([initialLat, initialLon]).addTo(map);

    function notifyHost(lat, lon) {
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({ type: 'picked', lat, lon });
      }
    }

    map.on('click', function(e) {
      const lat = e.latlng.lat;
      const lon = e.latlng.lng;
      marker.setLatLng([lat, lon]);
      notifyHost(lat, lon);
    });

    notifyHost(initialLat, initialLon);
  </script>
</body>
</html>
""";
    }
}
