namespace BinanceScannerApp.Strategies;

/// <summary>A hypothetical plan shown by the scanner; it never sends an order to Binance.</summary>
public sealed record TradePlan(string Side, decimal Entry, decimal Stop, decimal Target, int EntryIndex);
