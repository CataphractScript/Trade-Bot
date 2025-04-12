import MetaTrader5 as mt5
from datetime import datetime
import pandas as pd

if not mt5.initialize():
    print("Initialization failed:", mt5.last_error())
    quit()

symbol = "USDCHF"

mt5.symbol_select(symbol, True)

data = mt5.copy_rates_from_pos(symbol, mt5.TIMEFRAME_M15, 0, 20)

df = pd.DataFrame(data)

df.to_csv("OHCL.csv")

print(df)

mt5.shutdown()