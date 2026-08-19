namespace BinanceScannerApp.Models;

public sealed record AiMarketAnalysis(string Decision, int Confidence, string Reasoning, string DisplayText);
