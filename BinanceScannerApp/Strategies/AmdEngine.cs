using BinanceScannerApp.Models;

namespace BinanceScannerApp.Strategies;

/// <summary>Original AMD high-timeframe filter: 50/200 EMA crossover.</summary>
public sealed class AmdEngine
{
    public string GetBias(IReadOnlyList<Candle> candles)
    {
        if (candles.Count < 200) return "Neutral";
        var ema50 = Ema(candles.Select(c => c.Close), 50);
        var ema200 = Ema(candles.Select(c => c.Close), 200);
        return ema50 > ema200 ? "Bullish" : ema50 < ema200 ? "Bearish" : "Neutral";
    }

    private static decimal Ema(IEnumerable<decimal> values, int period)
    {
        var multiplier = 2m / (period + 1);
        decimal? ema = null;
        foreach (var value in values) ema = ema is null ? value : ema.Value + (value - ema.Value) * multiplier;
        return ema ?? 0;
    }
}
