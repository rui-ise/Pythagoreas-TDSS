using System.IO;
using System.Text.Json;

namespace BinanceScannerApp.Services;

public sealed class AiUsageStore
{
    private readonly string _path;
    private sealed record DailyUsage(DateOnly Date, int Count);

    public AiUsageStore()
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BinanceScannerApp");
        Directory.CreateDirectory(folder);
        _path = Path.Combine(folder, "ai-usage.json");
    }

    public int DailyLimit => int.TryParse(Environment.GetEnvironmentVariable("AI_DAILY_LIMIT"), out var limit) && limit > 0 ? limit : 12;
    public async Task<int> UsedTodayAsync()
    {
        if (!File.Exists(_path)) return 0;
        await using var stream = File.OpenRead(_path);
        var usage = await JsonSerializer.DeserializeAsync<DailyUsage>(stream);
        return usage?.Date == DateOnly.FromDateTime(DateTime.Now) ? usage.Count : 0;
    }
    public async Task<bool> TryConsumeAsync()
    {
        var used = await UsedTodayAsync();
        if (used >= DailyLimit) return false;
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, new DailyUsage(DateOnly.FromDateTime(DateTime.Now), used + 1));
        return true;
    }

    public async Task<int> RemainingAsync() => Math.Max(0, DailyLimit - await UsedTodayAsync());
}
