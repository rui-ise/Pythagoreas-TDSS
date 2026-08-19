namespace BinanceScannerApp.Models;

public sealed record MarketPrediction(
    Guid Id,
    DateTime CreatedAt,
    string Symbol,
    decimal PriceAtPrediction,
    MarketIndicators Indicators,
    string Decision,
    int Confidence,
    string Reasoning,
    DateTime CheckAfter,
    bool OutcomeChecked = false,
    decimal? OutcomePrice = null,
    bool? WasCorrect = null);
