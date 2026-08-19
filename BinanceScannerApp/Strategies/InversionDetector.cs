using BinanceScannerApp.Models;

namespace BinanceScannerApp.Strategies;

/// <summary>Finds the break, retest, and confirmation sequence which turns an FVG into an iFVG.</summary>
public sealed class InversionDetector
{
    public IReadOnlyList<InversionEvent> FindInversions(IReadOnlyList<Candle> candles, IReadOnlyList<FairValueGap> zones)
    {
        var events = new List<InversionEvent>();
        foreach (var zone in zones)
        {
            var inversion = FindInversion(candles, zone);
            if (inversion is null) continue;
            var direction = zone.Direction == "Bullish" ? "Short" : "Long";
            var retest = FindRetest(candles, zone, inversion.Value);
            if (retest is null) continue;
            var confirmation = FindConfirmation(candles, zone, retest.Value, direction);
            if (confirmation is not null) events.Add(new InversionEvent(zone, inversion.Value, confirmation.Value, direction));
        }
        return events;
    }

    private static int? FindInversion(IReadOnlyList<Candle> candles, FairValueGap zone)
    {
        for (var i = zone.Index + 1; i < candles.Count; i++)
            if ((zone.Direction == "Bullish" && candles[i].Close < zone.Bottom) || (zone.Direction == "Bearish" && candles[i].Close > zone.Top)) return i;
        return null;
    }
    private static int? FindRetest(IReadOnlyList<Candle> candles, FairValueGap zone, int start)
    {
        for (var i = start + 1; i < candles.Count; i++) if (candles[i].High >= zone.Bottom && candles[i].Low <= zone.Top) return i;
        return null;
    }
    private static int? FindConfirmation(IReadOnlyList<Candle> candles, FairValueGap zone, int start, string direction)
    {
        for (var i = start; i < candles.Count; i++)
            if ((direction == "Long" && candles[i].Close > zone.Top) || (direction == "Short" && candles[i].Close < zone.Bottom)) return i;
        return null;
    }
}
