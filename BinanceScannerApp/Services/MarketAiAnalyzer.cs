using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BinanceScannerApp.Models;

namespace BinanceScannerApp.Services;

/// <summary>Optional read-only market commentary through OpenRouter's OpenAI-compatible API.</summary>
public sealed class MarketAiAnalyzer : IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://openrouter.ai") };
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));

    public async Task<AiMarketAnalysis> AnalyzeAsync(ScanResult scan, MarketIndicators indicators, CancellationToken token)
    {
        var key = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("AI is not configured. Add OPENROUTER_API_KEY to .env, then restart the app.");
        var model = Environment.GetEnvironmentVariable("AI_MODEL") ?? "openrouter/free";
        var prompt = $"Return ONLY valid JSON: {{\"decision\":\"bullish|bearish|neutral\",\"confidence\":0-100,\"reasoning\":\"one cautious sentence\"}}. This is educational decision support, not financial advice. Use only this data: symbol={scan.Symbol}; price={scan.LastPrice}; 4H bias={scan.FourHourBias}; SMA5={indicators.SmaShort}; SMA20={indicators.SmaLong}; RSI14={indicators.Rsi}; bullish FVGs={scan.BullishGaps}; bearish FVGs={scan.BearishGaps}; confirmed inversions={scan.ConfirmedInversions}; setup={scan.Signal}; plan={scan.TradePlan}.";
        // Do not require provider-side structured-output support: the free router can select models
        // that only return ordinary text, while the prompt and parser still preserve a usable result.
        var payload = JsonSerializer.Serialize(new { model, messages = new[] { new { role = "user", content = prompt } }, max_tokens = 180, temperature = 0.2 });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/chat/completions") { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        using var response = await _http.SendAsync(request, token);
        var json = await response.Content.ReadAsStringAsync(token);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"AI request failed ({(int)response.StatusCode}): {ReadError(json)}");
        using var document = JsonDocument.Parse(json);
        var content = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? throw new InvalidOperationException("AI returned no text.");
        return ParseAnalysis(content);
    }

    private static string ReadError(string json)
    {
        try { using var doc = JsonDocument.Parse(json); return doc.RootElement.GetProperty("error").GetProperty("message").GetString() ?? "Unknown error"; }
        catch { return "Unknown error"; }
    }

    private static AiMarketAnalysis ParseAnalysis(string content)
    {
        var firstBrace = content.IndexOf('{');
        var lastBrace = content.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            try
            {
                using var analysis = JsonDocument.Parse(content[firstBrace..(lastBrace + 1)]);
                var root = analysis.RootElement;
                var decision = root.TryGetProperty("decision", out var value) ? value.GetString()?.ToLowerInvariant() ?? "neutral" : "neutral";
                if (decision is not ("bullish" or "bearish" or "neutral")) decision = "neutral";
                var confidence = root.TryGetProperty("confidence", out var confidenceValue) && confidenceValue.TryGetInt32(out var parsedConfidence) ? Math.Clamp(parsedConfidence, 0, 100) : 50;
                var reasoning = root.TryGetProperty("reasoning", out var reason) ? reason.GetString() ?? content : content;
                return new AiMarketAnalysis(decision, confidence, reasoning, $"{decision.ToUpperInvariant()} ({confidence}% confidence) - {reasoning}");
            }
            catch (JsonException) { /* fall back to plain text below */ }
        }
        return new AiMarketAnalysis("neutral", 50, content, $"AI note (unstructured): {content}");
    }
    public void Dispose() => _http.Dispose();
}
