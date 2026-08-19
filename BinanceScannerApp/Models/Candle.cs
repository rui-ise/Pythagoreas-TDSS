namespace BinanceScannerApp.Models;

public sealed record Candle(DateTime OpenTime, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
