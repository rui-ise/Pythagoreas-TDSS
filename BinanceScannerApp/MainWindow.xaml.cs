using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using BinanceScannerApp.Models;
using BinanceScannerApp.Services;

namespace BinanceScannerApp;

public partial class MainWindow : Window
{
    private readonly BinanceClient _binance = new();
    private readonly StrategyScanner _scanner = new();
    private readonly HistoryStore _history = new();
    private readonly MarketAiAnalyzer _ai = new();
    private readonly AiUsageStore _aiUsage = new();
    private readonly IndicatorCalculator _indicators = new();
    private readonly PredictionStore _predictions = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromHours(4) };
    private IReadOnlyList<Candle> _latestCandles = [];
    private bool _isScanning;

    public MainWindow()
    {
        EnvironmentLoader.Load();
        InitializeComponent();
        Loaded += async (_, _) => { await RefreshHistoryAsync(); await UpdateAiUsageAsync(); await ScanAsync(); };
        Closed += (_, _) => { _binance.Dispose(); _ai.Dispose(); };
        _timer.Tick += async (_, _) => await ScanAsync();
        _timer.Start();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) => await ScanAsync();
    private async void RefreshChart_Click(object sender, RoutedEventArgs e) => await ScanAsync();

    private async Task ScanAsync()
    {
        if (_isScanning) return;
        var symbol = SymbolBox.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(symbol)) { StatusText.Text = "Enter a Binance symbol, for example BTCUSDT."; return; }
        _isScanning = true;
        ScanButton.IsEnabled = false;
        StatusText.Text = $"Fetching {symbol} public market data...";
        try
        {
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var priceTask = _binance.GetLastPriceAsync(symbol, cancel.Token);
            var fourHourTask = _binance.GetCandlesAsync(symbol, "4h", 250, cancel.Token);
            var fifteenMinuteTask = _binance.GetCandlesAsync(symbol, "15m", 200, cancel.Token);
            await Task.WhenAll(priceTask, fourHourTask, fifteenMinuteTask);
            _latestCandles = await fifteenMinuteTask;
            var result = _scanner.Scan(symbol, await priceTask, await fourHourTask, _latestCandles);
            var indicatorValues = _indicators.Calculate(_latestCandles);
            await GradeDuePredictionsAsync(symbol, result.LastPrice);
            var aiStatus = "";
            if (_ai.IsConfigured)
            {
                if (await _aiUsage.RemainingAsync() > 0)
                {
                    AiAnalysisText.Text = "AI is reviewing the latest market scan...";
                    try
                    {
                        var analysis = await _ai.AnalyzeAsync(result, indicatorValues, cancel.Token);
                        result = result with { AiAnalysis = analysis.DisplayText };
                        await _aiUsage.TryConsumeAsync();
                        await _predictions.AddAsync(new MarketPrediction(Guid.NewGuid(), DateTime.Now, symbol, result.LastPrice, indicatorValues,
                            analysis.Decision, analysis.Confidence, analysis.Reasoning, DateTime.Now.AddHours(PredictionCheckHours)));
                    }
                    catch (Exception ex)
                    {
                        aiStatus = $" Market data was saved, but AI is unavailable: {ex.Message}";
                        result = result with { AiAnalysis = $"AI unavailable: {ex.Message}" };
                    }
                }
                else
                {
                    result = result with { AiAnalysis = "The local daily AI safety limit has been reached. It resets tomorrow." };
                }
            }
            else
            {
                result = result with { AiAnalysis = "AI is not configured. Copy .env.example to .env, add OPENROUTER_API_KEY, then restart the app." };
            }
            PriceText.Text = $"Last price: {result.LastPrice:N2} USDT";
            BiasText.Text = $"4H trend: {result.FourHourBias}";
            SignalText.Text = $"Scanner result: {result.Signal}";
            GapText.Text = $"15m gaps: {result.BullishGaps} bullish / {result.BearishGaps} bearish  |  {result.ConfirmedInversions} confirmed inversions";
            PlanText.Text = result.TradePlan;
            AiAnalysisText.Text = result.AiAnalysis;
            StatusText.Text = $"Scanned {result.ScannedAt:yyyy-MM-dd HH:mm:ss}. Saved locally.{aiStatus}";
            await _history.AddAsync(result);
            await RefreshHistoryAsync();
            await UpdateAiUsageAsync();
            DrawChart();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not retrieve Binance data: {ex.Message}";
        }
        finally { _isScanning = false; ScanButton.IsEnabled = true; }
    }

    private async Task RefreshHistoryAsync() => HistoryGrid.ItemsSource = await _predictions.LoadAsync();
    private static int PredictionCheckHours => int.TryParse(Environment.GetEnvironmentVariable("PREDICTION_CHECK_HOURS"), out var value) && value > 0 ? value : 24;
    private async Task GradeDuePredictionsAsync(string symbol, decimal currentPrice)
    {
        var predictions = await _predictions.LoadAsync();
        var changed = false;
        for (var i = 0; i < predictions.Count; i++)
        {
            var prediction = predictions[i];
            if (prediction.OutcomeChecked || prediction.Symbol != symbol || prediction.CheckAfter > DateTime.Now) continue;
            var movedUp = currentPrice > prediction.PriceAtPrediction;
            var correct = (prediction.Decision == "bullish" && movedUp) || (prediction.Decision == "bearish" && !movedUp);
            predictions[i] = prediction with { OutcomeChecked = true, OutcomePrice = currentPrice, WasCorrect = correct };
            changed = true;
        }
        if (changed) await _predictions.SaveAsync(predictions);
    }
    private async Task UpdateAiUsageAsync()
    {
        if (!_ai.IsConfigured) { AiUsageText.Text = "AI: not configured"; return; }
        AiUsageText.Text = $"AI: {await _aiUsage.RemainingAsync()} / {_aiUsage.DailyLimit} uses left today";
    }
    private void PriceChart_SizeChanged(object sender, SizeChangedEventArgs e) => DrawChart();

    private void DrawChart()
    {
        PriceChart.Children.Clear();
        if (_latestCandles.Count < 2 || PriceChart.ActualWidth < 2 || PriceChart.ActualHeight < 2) return;
        var prices = _latestCandles.Select(c => c.Close).ToList();
        var min = prices.Min(); var max = prices.Max(); var range = max - min;
        if (range == 0) range = 1;
        var line = new Polyline { Stroke = Brushes.DodgerBlue, StrokeThickness = 2 };
        for (var i = 0; i < prices.Count; i++)
        {
            var x = i * PriceChart.ActualWidth / (prices.Count - 1);
            var y = PriceChart.ActualHeight - ((double)((prices[i] - min) / range) * PriceChart.ActualHeight);
            line.Points.Add(new Point(x, y));
        }
        PriceChart.Children.Add(line);
    }
}
