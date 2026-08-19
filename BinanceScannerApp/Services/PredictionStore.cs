using System.IO;
using System.Text.Json;
using BinanceScannerApp.Models;

namespace BinanceScannerApp.Services;

/// <summary>Local JSON replacement for the reference project's Supabase predictions table.</summary>
public sealed class PredictionStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    public PredictionStore()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BinanceScannerApp");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "predictions.json");
    }
    public async Task<List<MarketPrediction>> LoadAsync()
    {
        if (!File.Exists(_path)) return [];
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<MarketPrediction>>(stream, _json) ?? [];
    }
    public async Task SaveAsync(List<MarketPrediction> predictions)
    {
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, predictions.OrderByDescending(p => p.CreatedAt).Take(500).ToList(), _json);
    }
    public async Task AddAsync(MarketPrediction prediction)
    {
        var values = await LoadAsync(); values.Insert(0, prediction); await SaveAsync(values);
    }
}
