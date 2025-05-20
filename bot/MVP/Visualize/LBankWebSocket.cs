using Newtonsoft.Json;
using System;
using System.Drawing.Text;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Visualize
{
    internal static class LBankWebSocket
    {
        private static readonly Uri ServerUri = new Uri("wss://www.lbkex.net/ws/V2/");
        //private static List<KbarData> kbarBuffer = new List<KbarData>();
        //private static DateTime currentMinute = DateTime.MinValue;

        private static KbarData? lastKbar = null;
        private static DateTime currentMinute = DateTime.MinValue;

        private static readonly string CsvFilePath = "candles.csv";



        // Loop to keep trying reconnect on disconnection
        public static async Task ListenToLBank()
        {

            // Loop to keep trying reconnect on disconnection
            while (true)
            {

                using (ClientWebSocket websocket = new ClientWebSocket())
                {
                    try
                    {
                        Logger.Log("Connecting to WebSocket...");
                        await websocket.ConnectAsync(ServerUri, CancellationToken.None);
                        Logger.Log("Connected to WebSocket.");

                        // Subscription message for kbar data
                        var requestMessage = new
                        {
                            action = "subscribe",
                            subscribe = "kbar",
                            kbar = "1min",
                            pair = "btc_usdt"
                        };

                        // Send subscription request
                        var jsonMessage = JsonConvert.SerializeObject(requestMessage);
                        var buffer = Encoding.UTF8.GetBytes(jsonMessage);
                        await websocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        Logger.Log("Subscription request sent.");

                        var receiveBuffer = new byte[4096];
                        var re = new char[4096];

                        // Listen to messages while connection is open
                        while (websocket.State == WebSocketState.Open)
                        {
                            var result = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                            // If server sends a close message, close the connection gracefully
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                Logger.Log("Server initiated close. Closing WebSocket...", Color.Red);
                                await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", CancellationToken.None);
                                break;
                            }
                            // Convert received bytes to string
                            var message = Encoding.UTF8.GetString(receiveBuffer);
                            Logger.Log($"Received message: {message}", Color.Aqua);

                            try
                            {
                                // Deserialize message into a dictionary
                                var deserializeMessage = JsonConvert.DeserializeObject<ApiResponse>(message);

                                if (deserializeMessage != null &&
                                    deserializeMessage.type == "kbar" &&
                                    deserializeMessage.kbar != null &&
                                    !string.IsNullOrEmpty(deserializeMessage.pair) &&
                                    !string.IsNullOrEmpty(deserializeMessage.SERVER) &&
                                    deserializeMessage.TS != DateTime.MinValue)
                                {

                                    if (deserializeMessage?.kbar != null)
                                    {
                                        var kbar = deserializeMessage.kbar;
                                        //DateTime kbarTime = kbar.t;
                                        DateTime kbarTime = new DateTime(kbar.t.Year, kbar.t.Month, kbar.t.Day, kbar.t.Hour, kbar.t.Minute, 0);

                                        // If we moved into a new minute
                                        if (kbarTime > currentMinute)
                                        {
                                            if (lastKbar != null)
                                            {
                                                Logger.Log($"[CANDLE] Time: {currentMinute:HH:mm}, O: {lastKbar.o}, H: {lastKbar.h}, L: {lastKbar.l}, C: {lastKbar.c}", Color.Yellow);
                                                SaveCandleToCsv(currentMinute, lastKbar.o, lastKbar.h, lastKbar.l, lastKbar.c);
                                            }
                                            currentMinute = kbarTime;
                                        }

                                        // Always update last received kbar for the current minute
                                        lastKbar = kbar;

                                        //// Initialize currentMinute if it hasn't been set yet
                                        //if (currentMinute == DateTime.MinValue)
                                        //    currentMinute = new DateTime(kbarTime.Year, kbarTime.Month, kbarTime.Day, kbarTime.Hour, kbarTime.Minute, 0);

                                        //// If the data is still within the same minute, add it to the buffer
                                        //if (kbarTime >= currentMinute && kbarTime < currentMinute.AddMinutes(1))
                                        //{
                                        //    kbarBuffer.Add(kbar);
                                        //}
                                        //else
                                        //{
                                        //    // Minute has changed, so create and log the candle
                                        //    if (kbarBuffer.Count > 0)
                                        //    {
                                        //        var open = kbarBuffer.First().o;
                                        //        var close = kbarBuffer.Last().c;
                                        //        var high = kbarBuffer.Max(k => k.h);
                                        //        var low = kbarBuffer.Min(k => k.l);

                                        //        Logger.Log($"[CANDLE] Time: {currentMinute:HH:mm}, O: {open}, H: {high}, L: {low}, C: {close}", Color.Yellow);

                                        //        SaveCandleToCsv(currentMinute, open, high, low, close);
                                        //    }

                                        //    // Clear the buffer for the new minute
                                        //    kbarBuffer.Clear();
                                        //    currentMinute = new DateTime(kbarTime.Year, kbarTime.Month, kbarTime.Day, kbarTime.Hour, kbarTime.Minute, 0);
                                        //    kbarBuffer.Add(kbar);
                                        //}
                                    }
                                }
                                else
                                {
                                    Logger.Log("Ignored non-kbar message.", Color.Green);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Error processing message: {ex.Message}", Color.Red);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"WebSocket error: {ex.Message}", Color.Red);
                    }

                }
                Logger.Log("Reconnecting in 5 seconds...");
                await Task.Delay(5000);
            }

        }



        // Save to CSV file
        private static void SaveCandleToCsv(DateTime time, double open, double high, double low, double close)
        {
            bool fileExists = File.Exists(CsvFilePath);

            using (var writer = new StreamWriter(CsvFilePath, append: true))
            {
                // If the file does not exist, write the header first
                if (!fileExists)
                {
                    writer.WriteLine("Time,Open,High,Low,Close");
                }

                // Append the new candle data
                writer.WriteLine($"{time:yyyy-MM-dd HH:mm},{open},{high},{low},{close}");
            }
        }
    }
}
