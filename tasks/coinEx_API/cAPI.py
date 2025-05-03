import requests

symbol = "BTCUSDT"
url = f"https://api.coinex.com/v1/market/ticker?market={symbol}"

response = requests.get(url)
data = response.json()

if data["code"] == 0:
    ticker = data["data"]["ticker"]
    print(f"💰 {symbol} Price Info:")
    print("Last:", ticker["last"])
    print("High:", ticker["high"])
    print("Low:", ticker["low"])
    print("Volume:", ticker["vol"])
else:
    print("❌ Error:", data["message"])
