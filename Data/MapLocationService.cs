using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Eco_Matic.Data;

public sealed class MapLocationResult
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Address { get; init; } = string.Empty;
}

public sealed class MapLocationService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public static MapLocationService Instance { get; } = new();

    private MapLocationService()
    {
    }

    public async Task<MapLocationResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        string requestUri =
            $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&zoom=18&addressdetails=1";

        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        JsonElement root = document.RootElement;

        string address = root.TryGetProperty("display_name", out JsonElement displayNameElement)
            ? displayNameElement.GetString() ?? string.Empty
            : string.Empty;

        return new MapLocationResult
        {
            Latitude = latitude,
            Longitude = longitude,
            Address = address
        };
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("EcoMatic", "1.0"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(student-project)"));
        return client;
    }
}
