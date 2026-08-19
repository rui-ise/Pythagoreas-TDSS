using BinanceScannerApp.Models;

namespace BinanceScannerApp.Strategies;

public sealed class TradeGenerator
{
    private const decimal RiskReward = 2m;
    private const decimal StopBufferPercent = .1m;

    public TradePlan CreateFromInversion(InversionEvent setup, IReadOnlyList<Candle> candles)
    {
        var entry = candles[setup.ConfirmationIndex].Close;
        var buffer = setup.Zone.Height * StopBufferPercent;
        if (setup.Direction == "Long")
        {
            var stop = setup.Zone.Bottom - buffer;
            return new TradePlan("Long", entry, stop, entry + (entry - stop) * RiskReward, setup.ConfirmationIndex);
        }
        var shortStop = setup.Zone.Top + buffer;
        return new TradePlan("Short", entry, shortStop, entry - (shortStop - entry) * RiskReward, setup.ConfirmationIndex);
    }
}
