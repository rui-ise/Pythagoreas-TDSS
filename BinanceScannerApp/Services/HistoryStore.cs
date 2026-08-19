using System.IO;
using System.Text.Json;
using BinanceScannerApp.Models;

namespace BinanceScannerApp.Services;

/// <summary>Stores scan history in the user's application-data folder so updates do not overwrite the project.</summary>
public sealed class HistoryStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public HistoryStore()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BinanceScannerApp");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "scan-history.json");
    }

    public async Task<IReadOnlyList<ScanResult>> LoadAsync()
    {
        if (!File.Exists(_path)) return [];
        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<List<ScanResult>>(stream, _json) ?? [];
    }

    public async Task AddAsync(ScanResult result)
    {
        var history = (await LoadAsync()).ToList();
        history.Insert(0, result);
        history = history.Take(500).ToList();
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, history, _json);
    }
}
