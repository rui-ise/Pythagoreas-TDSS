namespace BinanceScannerApp.Strategies;

public sealed record FairValueGap(decimal Top, decimal Bottom, string Direction, int Index)
{
    public decimal Height => Top - Bottom;
}
