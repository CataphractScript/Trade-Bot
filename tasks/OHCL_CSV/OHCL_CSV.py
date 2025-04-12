import MetaTrader5 as mt5
from datetime import datetime
import pandas as pd

if not mt5.initialize():
    print("Initialization failed:", mt5.last_error())
    quit()

symbol = "EURUSD"

mt5.symbol_select(symbol, True)

rates = mt5.copy_rates_from_pos(symbol, mt5.TIMEFRAME_M15, 0, 20)

ohcl = pd.DataFrame(rates)

ohcl.to_csv('data.csv')