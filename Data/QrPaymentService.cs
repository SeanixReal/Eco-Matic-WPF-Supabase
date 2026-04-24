using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Eco_Matic.Data;

public sealed class QrPaymentService
{
    private static readonly Lazy<QrPaymentService> LazyInstance = new(() => new QrPaymentService());
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    public static QrPaymentService Instance => LazyInstance.Value;

    private QrPaymentService()
    {
    }

    public async Task<QrPaymentIntent> CreateIntentAsync(int machineId, decimal amount)
    {
        string url = SupabaseClient.Instance.GetFunctionUrl("qr-payment-confirm");
        string body = JsonSerializer.Serialize(new
        {
            machine_id = machineId,
            amount
        }, SupabaseClient.JsonOpts);

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(url, content);
        string json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        return new QrPaymentIntent(
            root.GetProperty("reference").GetString() ?? string.Empty,
            root.GetProperty("token").GetString() ?? string.Empty,
            root.GetProperty("confirm_url").GetString() ?? string.Empty);
    }

    public async Task<QrPaymentStatus> GetStatusAsync(string reference, string token)
    {
        string baseUrl = SupabaseClient.Instance.GetFunctionUrl("qr-payment-confirm");
        string url = $"{baseUrl}?status=1&ref={Uri.EscapeDataString(reference)}&token={Uri.EscapeDataString(token)}";

        using var response = await _http.GetAsync(url);
        string json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        return new QrPaymentStatus(
            root.GetProperty("reference").GetString() ?? reference,
            root.GetProperty("status").GetString() ?? "pending",
            root.GetProperty("amount").GetDecimal());
    }

    public async Task<QrPaymentStatus> MarkPaidAsync(string reference, string token, decimal amount)
    {
        string url = SupabaseClient.Instance.GetFunctionUrl("qr-payment-confirm");
        string body = JsonSerializer.Serialize(new
        {
            action = "pay",
            reference,
            token,
            amount
        }, SupabaseClient.JsonOpts);

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(url, content);
        string json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        return new QrPaymentStatus(
            root.GetProperty("reference").GetString() ?? reference,
            root.GetProperty("status").GetString() ?? "paid",
            root.GetProperty("amount").GetDecimal());
    }
}

public sealed record QrPaymentIntent(string Reference, string Token, string ConfirmUrl);

public sealed record QrPaymentStatus(string Reference, string Status, decimal Amount);
