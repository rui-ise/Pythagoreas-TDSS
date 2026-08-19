# Pythagoreas TDSS

Pythagoreas TDSS is a Windows desktop app that collects public Binance market data for a chosen symbol (default: `BTCUSDT`), calculates technical and price-action signals locally, optionally asks an OpenRouter AI model for a cautious summary, and keeps a local record of the resulting predictions.

It is an educational market-scanning tool. It is **not** connected to a Binance account, cannot place orders, and does not provide financial advice.

## What it does

On each scan, the application:

1. Retrieves the current symbol price from Binance.
2. Retrieves 250 four-hour candles and 200 fifteen-minute candles from Binance.
3. Calculates local indicators from the fifteen-minute closes: SMA(5), SMA(20), and RSI(14).
4. Applies the original backtester strategy: 4H 50/200 EMA trend bias, three-candle Fair Value Gaps, iFVG inversion/retest confirmation, and a hypothetical 2R entry/stop/target plan.
5. If OpenRouter is configured, sends the scan data to the selected model for a directional opinion, confidence estimate, and brief cautionary reasoning.
6. Saves predictions locally and checks them later against the then-current Binance price.

The app polls Binance at startup, when you select a refresh button, and every four hours while it is open. It does not hold a continuous WebSocket price stream open.

## Tabs

| Tab                       | Purpose                                                                                                               |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| **Overview**              | Current Binance price, 4H bias, iFVG setup status, hypothetical trade plan, AI notes, and the local AI-use allowance. |
| **Binance Charts**        | A simple chart of the latest 200 fifteen-minute closing prices.                                                       |
| **AI Prediction History** | Local AI predictions, their indicator snapshots, and outcome checks once they are due.                                |

## Requirements

- Windows 10 or Windows 11
- Visual Studio 2022 with the **.NET desktop development** workload
- .NET 10 SDK, or the matching SDK selected by Visual Studio
- Internet access for Binance data; OpenRouter access is optional

## Run it in Visual Studio

1. Open `BinanceScannerApp.csproj` in Visual Studio.
2. Allow Visual Studio to restore project dependencies if it asks.
3. Choose **Debug** and your normal local platform configuration.
4. Press **F5** to run with the debugger, or **Ctrl+F5** to run without it.
5. Enter a Binance spot-market symbol such as `BTCUSDT`, then select **Scan market**.

If Visual Studio says the output executable is in use, stop the running debug session with **Shift+F5**, then build again.

## Optional OpenRouter AI configuration

The scanner works without AI. To enable AI notes and prediction history:

1. Create a file named `.env` in the same folder as `BinanceScannerApp.csproj`.
2. Copy the fields from `.env.example`.
3. Add your OpenRouter API key and save the file.
4. Restart the application.

```env
OPENROUTER_API_KEY=your_key_here
AI_MODEL=openrouter/free
AI_DAILY_LIMIT=12
PREDICTION_CHECK_HOURS=24
```

| Variable                 | Meaning                                                                                        |
| ------------------------ | ---------------------------------------------------------------------------------------------- |
| `OPENROUTER_API_KEY`     | Your private OpenRouter API key. Never commit or share this file.                              |
| `AI_MODEL`               | An OpenRouter model slug. `openrouter/free` is a low-cost starting point.                      |
| `AI_DAILY_LIMIT`         | A local safety limit for AI calls per day, not your OpenRouter account quota or billing limit. |
| `PREDICTION_CHECK_HOURS` | Hours before the app grades a prediction against the current price. The default is 24.         |

OpenRouter errors do not stop Binance scanning. The Overview tab retains the market data and explains the AI error separately.

| File                                                 | Contents                                                 |
| ---------------------------------------------------- | -------------------------------------------------------- |
| `%LocalAppData%\BinanceScannerApp\scan-history.json` | Past raw scan summaries.                                 |
| `%LocalAppData%\BinanceScannerApp\predictions.json`  | AI predictions, indicator snapshots, and outcome checks. |
| `%LocalAppData%\BinanceScannerApp\ai-usage.json`     | Local daily AI-use counter.                              |

## Scheduling

While the app is open, it scans every four hours using its internal timer. For a Windows scheduled launch, publish the project, update the executable path in `scripts/Register-EveryFourHours.ps1`, then run that script in PowerShell.

## Project layout

```text
BinanceScannerApp/
├── Models/       Data objects: candles, scans, indicators, predictions
├── Services/     Binance client, AI client, local persistence, indicators
├── Strategies/   AMD, FVG, iFVG, and hypothetical trade-plan logic
├── scripts/      Optional Windows scheduling helper
├── MainWindow.*  WPF user interface and scan workflow
├── .env.example  Safe configuration template
└── README.md     This guide
```

## Important limitations

- A successful scan or AI opinion does not establish a profitable trade.
- The chart is a closing-price line chart, not a trading platform.
- Historical prediction grading checks whether price was higher or lower after the configured time; it does not simulate stop/target execution.

## Future Plans

- GUI improvements
- Auto-Scan toggle
- More strategies from professionals
- Ollama AI model (when I get my own GPU)

## License

Feel free to copy my code and do whatever.
