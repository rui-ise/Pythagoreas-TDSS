namespace BinanceScannerApp.Models;

public sealed record MarketIndicators(decimal CurrentPrice, decimal? SmaShort, decimal? SmaLong, decimal? Rsi);
