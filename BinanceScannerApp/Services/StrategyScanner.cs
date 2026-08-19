using BinanceScannerApp.Models;
using BinanceScannerApp.Strategies;

namespace BinanceScannerApp.Services;

/// <summary>Produces a current market scan. This is a signal scanner, not an order-execution system.</summary>
public sealed class StrategyScanner
{
    private readonly AmdEngine _amd = new();
    private readonly IfvgDetector _gaps = new();
    private readonly InversionDetector _inversions = new();
    private readonly TradeGenerator _trades = new();

    public ScanResult Scan(string symbol, decimal lastPrice, IReadOnlyList<Candle> fourHour, IReadOnlyList<Candle> fifteenMinute)
    {
        var bias = _amd.GetBias(fourHour);
        var zones = _gaps.FindAll(fifteenMinute);
        var inversions = _inversions.FindInversions(fifteenMinute, zones);
        var aligned = inversions.Where(e => (bias == "Bullish" && e.Direction == "Long") || (bias == "Bearish" && e.Direction == "Short")).ToList();
        var latest = aligned.OrderByDescending(e => e.ConfirmationIndex).FirstOrDefault();
        var plan = latest is null ? null : _trades.CreateFromInversion(latest, fifteenMinute);
        var signal = plan is null ? "No aligned confirmed iFVG setup" : $"{plan.Side.ToUpperInvariant()} setup confirmed";
        var planText = plan is null ? "No trade plan" : $"Entry {plan.Entry:N2}   Stop {plan.Stop:N2}   Target {plan.Target:N2} (2R)";

        return new ScanResult(DateTime.Now, symbol, lastPrice, bias,
            zones.Count(z => z.Direction == "Bullish"), zones.Count(z => z.Direction == "Bearish"), inversions.Count, signal, planText, "AI analysis pending.",
            "A signal is informational only. Confirm risk, liquidity, and execution rules before trading.");
    }
}
