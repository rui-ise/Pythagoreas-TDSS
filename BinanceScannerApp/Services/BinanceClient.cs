using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using BinanceScannerApp.Models;

namespace BinanceScannerApp.Services;

/// <summary>Read-only client for Binance's public market-data endpoints. No account credentials are used.</summary>
public sealed class BinanceClient : IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://api.binance.com") };

    public async Task<IReadOnlyList<Candle>> GetCandlesAsync(string symbol, string interval, int limit, CancellationToken token)
    {
        var url = $"/api/v3/klines?symbol={Uri.EscapeDataString(symbol.ToUpperInvariant())}&interval={interval}&limit={limit}";
        using var response = await _http.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(token);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: token);

        return document.RootElement.EnumerateArray().Select(row => new Candle(
            DateTimeOffset.FromUnixTimeMilliseconds(row[0].GetInt64()).UtcDateTime,
            ParseDecimal(row[1]), ParseDecimal(row[2]), ParseDecimal(row[3]), ParseDecimal(row[4]), ParseDecimal(row[5]))).ToList();
    }

    public async Task<decimal> GetLastPriceAsync(string symbol, CancellationToken token)
    {
        using var response = await _http.GetAsync($"/api/v3/ticker/price?symbol={Uri.EscapeDataString(symbol.ToUpperInvariant())}", token);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(token);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: token);
        return ParseDecimal(document.RootElement.GetProperty("price"));
    }

    private static decimal ParseDecimal(JsonElement element) => decimal.Parse(element.GetString()!, CultureInfo.InvariantCulture);
    public void Dispose() => _http.Dispose();
}
