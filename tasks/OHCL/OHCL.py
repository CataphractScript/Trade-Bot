import MetaTrader5 as mt5
from datetime import datetime

# Initialize connection to MetaTrader 5 terminal
if not mt5.initialize():
    print("Initialization failed:", mt5.last_error())
    quit()

# Select the trading symbol (e.g., EURUSD)
symbol = "EURUSD"
mt5.symbol_select(symbol, True)

# Request 10 latest 1-minute candles (OHLC data)
rates = mt5.copy_rates_from_pos(symbol, mt5.TIMEFRAME_M1, 0, 10)

# Loop through and print each candle's OHLC data with timestamp
for r in rates:
    print(f"Time: {datetime.fromtimestamp(r['time'])}, Open: {r['open']}, High: {r['high']}, Low: {r['low']}, Close: {r['close']}")

# Shut down the connection to MetaTrader 5
mt5.shutdown()
