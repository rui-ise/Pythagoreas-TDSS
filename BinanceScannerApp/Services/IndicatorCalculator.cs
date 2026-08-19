using BinanceScannerApp.Models;

namespace BinanceScannerApp.Services;

/// <summary>Computes the reference pipeline's SMA and RSI values locally from Binance closes.</summary>
public sealed class IndicatorCalculator
{
    public MarketIndicators Calculate(IReadOnlyList<Candle> candles)
    {
        var closes = candles.Select(c => c.Close).ToList();
        if (closes.Count == 0) throw new ArgumentException("At least one candle is required.");
        decimal? shortSma = closes.Count >= 5 ? closes.TakeLast(5).Average() : null;
        decimal? longSma = closes.Count >= 20 ? closes.TakeLast(20).Average() : null;
        decimal? rsi = CalculateRsi(closes, 14);
        return new MarketIndicators(closes[^1], shortSma, longSma, rsi);
    }

    private static decimal? CalculateRsi(IReadOnlyList<decimal> closes, int period)
    {
        if (closes.Count <= period) return null;
        var gains = new List<decimal>(); var losses = new List<decimal>();
        for (var i = closes.Count - period; i < closes.Count; i++)
        {
            var delta = closes[i] - closes[i - 1];
            gains.Add(Math.Max(delta, 0)); losses.Add(Math.Max(-delta, 0));
        }
        var averageLoss = losses.Average();
        if (averageLoss == 0) return 100;
        var rs = gains.Average() / averageLoss;
        return 100m - (100m / (1m + rs));
    }
}
