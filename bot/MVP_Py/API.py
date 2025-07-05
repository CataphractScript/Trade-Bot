import requests
from datetime import datetime
import time
import pandas as pd

class api:
    def __init__(self, csv_path):
        self.csv_path = csv_path

    def rest_api(self, symbol="btc_usdt", time_frame="minute1", size=100):
        # LBank REST API endpoint for candlestick data
        url = "https://api.lbkex.com/v2/kline.do"

        # # Generate the timestamp for 'size' candles ago (each candle is 60 seconds for 1-minute timeframe)
        time_stamp = int(time.time()) - (60 * size)

        # Request parameters
        params = {
            "symbol": symbol,
            "size": size,
            "type": time_frame,
            "time": time_stamp,
        }

        try:
            # Send GET request to LBank API
            response = requests.get(url, params=params)
            response.raise_for_status()

            # Parse the JSON response
            data = dict(response.json())

            # Print full server response for debugging
            print("\nFull response from server:", data)

            # Check if the response was successful
            if data.get("result") != "true":
                print("❌ API Error:", data.get("msg", "Unknown error"))
                return  # Or use: raise Exception(data["msg"])

            # Extract candlestick data from the response
            candles = data["data"]

            # Convert list of lists to a DataFrame with column names
            df = pd.DataFrame(candles, columns=["Timestamp", "Open", "High", "Low", "Close", "Volume"])

            # Convert timestamp column from seconds to datetime format
            df["Timestamp"] = pd.to_datetime(df["Timestamp"], unit='s')

            # Save DataFrame to CSV file
            df.to_csv(self.csv_path, index=False)

        except requests.exceptions.ConnectionError as ce:
            print(f"Connection error: {ce}")
