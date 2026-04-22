using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Eco_Matic.Data;

/// <summary>
/// Lightweight Supabase REST client for the Eco-Matic WPF app.
/// Uses the PostgREST API exposed by Supabase.
/// ESP32 devices can use the same URL/key with HTTP requests.
/// </summary>
public class SupabaseClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    // ── Configuration ──────────────────────────────────────
    // Your Supabase project URL and anon key.
    // The anon key is safe to embed in desktop/IoT apps when RLS policies are set.
    private const string SUPABASE_URL = "https://woyadcahjkutrowkzryv.supabase.co";
    private const string SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndveWFkY2Foamt1dHJvd2t6cnl2Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzY2MDQwOTYsImV4cCI6MjA5MjE4MDA5Nn0.JJmpv7WUo6WeFdqifaf4U-FMPU2u8XQiNOrOXJ-h67g";
    // ───────────────────────────────────────────────────────

    private static readonly Lazy<SupabaseClient> _instance = new(() => new SupabaseClient());
    public static SupabaseClient Instance => _instance.Value;

    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private SupabaseClient()
    {
        _baseUrl = $"{SUPABASE_URL}/rest/v1";
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        _http.DefaultRequestHeaders.Add("apikey", SUPABASE_ANON_KEY);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SUPABASE_ANON_KEY);
        _http.DefaultRequestHeaders.Add("Prefer", "return=representation");
    }

    // ── Core HTTP Methods ──────────────────────────────────

    /// <summary>GET rows from a table with optional PostgREST query params.</summary>
    public async Task<JsonArray> GetAsync(string table, string queryParams = "")
    {
        string url = $"{_baseUrl}/{table}?{queryParams}";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)?.AsArray() ?? new JsonArray();
    }

    /// <summary>POST (insert) rows into a table.</summary>
    public async Task<JsonArray> PostAsync(string table, object body)
    {
        string url = $"{_baseUrl}/{table}";
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)?.AsArray() ?? new JsonArray();
    }

    /// <summary>PATCH (update) rows matching the filter.</summary>
    public async Task<JsonArray> PatchAsync(string table, string filter, object body)
    {
        string url = $"{_baseUrl}/{table}?{filter}";
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)?.AsArray() ?? new JsonArray();
    }

    /// <summary>DELETE rows matching the filter.</summary>
    public async Task DeleteAsync(string table, string filter)
    {
        string url = $"{_baseUrl}/{table}?{filter}";
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Call a Postgres RPC function.</summary>
    public async Task<string> RpcAsync(string functionName, object? body = null)
    {
        string url = $"{_baseUrl}/rpc/{functionName}";
        var content = new StringContent(
            body != null ? JsonSerializer.Serialize(body, JsonOpts) : "{}",
            Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// GET a single scalar value (e.g. count).
    /// Uses the Accept: application/vnd.pgrst.object+json header with count.
    /// </summary>
    public async Task<int> CountAsync(string table, string filter = "")
    {
        string url = $"{_baseUrl}/{table}?select=count&{filter}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Prefer", "count=exact");
        // Override default Accept header
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        // Parse count from Content-Range header
        if (response.Content.Headers.Contains("Content-Range"))
        {
            var range = response.Content.Headers.GetValues("Content-Range").FirstOrDefault() ?? "";
            var parts = range.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out int count))
                return count;
        }
        
        // Fallback: parse from response body
        var json = await response.Content.ReadAsStringAsync();
        var arr = JsonNode.Parse(json)?.AsArray();
        return arr?.Count ?? 0;
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            string url = $"{_baseUrl}/vending_machines?select=machine_id&limit=1";
            var response = await _http.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
