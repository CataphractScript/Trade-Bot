import MetaTrader5 as mt5
# from datetime import datetime
# import csv

if not mt5.initialize():
    print("Initialization failed:", mt5.last_error())
    quit()