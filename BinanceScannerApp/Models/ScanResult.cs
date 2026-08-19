namespace BinanceScannerApp.Models;

public sealed record ScanResult(
    DateTime ScannedAt,
    string Symbol,
    decimal LastPrice,
    string FourHourBias,
    int BullishGaps,
    int BearishGaps,
    int ConfirmedInversions,
    string Signal,
    string TradePlan,
    string AiAnalysis,
    string Notes);
