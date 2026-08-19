namespace BinanceScannerApp.Strategies;

public sealed record InversionEvent(FairValueGap Zone, int InversionIndex, int ConfirmationIndex, string Direction);
