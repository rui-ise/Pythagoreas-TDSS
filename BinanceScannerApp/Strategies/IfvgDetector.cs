using BinanceScannerApp.Models;

namespace BinanceScannerApp.Strategies;

/// <summary>Finds the original project's standard three-candle fair-value gaps.</summary>
public sealed class IfvgDetector
{
    public IReadOnlyList<FairValueGap> FindAll(IReadOnlyList<Candle> candles)
    {
        var zones = new List<FairValueGap>();
        for (var i = 2; i < candles.Count; i++)
        {
            if (candles[i - 2].High < candles[i].Low)
                zones.Add(new FairValueGap(candles[i].Low, candles[i - 2].High, "Bullish", i));
            else if (candles[i - 2].Low > candles[i].High)
                zones.Add(new FairValueGap(candles[i - 2].Low, candles[i].High, "Bearish", i));
        }
        return zones;
    }
}
