using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Visualize
{
    internal static class LBankWebSocket
    {
        private static readonly Uri ServerUri = new Uri("wss://www.lbkex.net/ws/V2/");
        private static KbarData? lastKbar = null;
        //private static DateTime currentMinute = DateTime.MinValue;

        private static List<KbarData> kbarBuffer = new List<KbarData>();
        private static DateTime currentMinute = DateTime.MinValue;

        private static readonly string CsvFilePath = "candles.csv";
        private static readonly object FileLock = new object(); // for thread-safe file access

        public static async Task ListenToLBank()
        {
            while (true) // Keep reconnecting if disconnected
            {
                using (ClientWebSocket websocket = new ClientWebSocket())
                {
                    try
                    {
                        Logger.Log("Connecting to WebSocket...");
                        await websocket.ConnectAsync(ServerUri, CancellationToken.None);
                        Logger.Log("Connected to WebSocket.");

                        var requestMessage = new
                        {
                            action = "subscribe",
                            subscribe = "kbar",
                            kbar = "1min",
                            pair = "btc_usdt"
                        };

                        var jsonMessage = JsonConvert.SerializeObject(requestMessage);
                        var buffer = Encoding.UTF8.GetBytes(jsonMessage);
                        await websocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        Logger.Log("Subscription request sent.");

                        var receiveBuffer = new byte[4096];

                        while (websocket.State == WebSocketState.Open)
                        {
                            // Accumulate WebSocket message in case it's fragmented
                            var fullMessage = new StringBuilder();
                            WebSocketReceiveResult result;
                            do
                            {
                                result = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                                string partialMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                                fullMessage.Append(partialMessage);

                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    Logger.Log("Server initiated close. Closing WebSocket...", Color.Red);
                                    await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", CancellationToken.None);
                                    break;
                                }
                            } while (!result.EndOfMessage);

                            string message = fullMessage.ToString();
                            Logger.Log($"Received message: {message}", Color.Aqua);

                            // Handle Ping/Pong messages
                            if (message.Contains("\"action\":\"ping\""))
                            {
                                var json = JObject.Parse(message);
                                string pingId = json["ping"]?.ToString();
                                if (!string.IsNullOrEmpty(pingId))
                                {
                                    var pongMessage = new { action = "pong", pong = pingId };
                                    string pongJson = JsonConvert.SerializeObject(pongMessage);
                                    byte[] pongBuffer = Encoding.UTF8.GetBytes(pongJson);
                                    await websocket.SendAsync(new ArraySegment<byte>(pongBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                                    Logger.Log($"Pong sent for ping {pingId}", Color.Green);
                                    continue; // skip further processing of ping message
                                }
                            }

                            try
                            {
                                var deserializeMessage = JsonConvert.DeserializeObject<ApiResponse>(message);

                                if (deserializeMessage != null &&
                                    deserializeMessage.type == "kbar" &&
                                    deserializeMessage.kbar != null &&
                                    !string.IsNullOrEmpty(deserializeMessage.pair) &&
                                    !string.IsNullOrEmpty(deserializeMessage.SERVER) &&
                                    deserializeMessage.TS != DateTime.MinValue)
                                {
                                    var kbar = deserializeMessage.kbar;
                                    DateTime kbarTime = new DateTime(kbar.t.Year, kbar.t.Month, kbar.t.Day, kbar.t.Hour, kbar.t.Minute, 0);

                                    //// If a new minute starts, save the previous candle
                                    //if (kbarTime > currentMinute)
                                    //{
                                    //    if (lastKbar != null)
                                    //    {
                                    //        Logger.Log($"[CANDLE] Time: {currentMinute:HH:mm}, O: {lastKbar.o}, H: {lastKbar.h}, L: {lastKbar.l}, C: {lastKbar.c}", Color.Yellow);
                                    //        SaveCandleToCsv(currentMinute, lastKbar.o, lastKbar.h, lastKbar.l, lastKbar.c);
                                    //    }
                                    //    currentMinute = kbarTime;
                                    //}

                                    //// Update latest kbar for the current minute
                                    //lastKbar = kbar;


                                    // Initialize currentMinute if it hasn't been set yet
                                    if (currentMinute == DateTime.MinValue)
                                        currentMinute = new DateTime(kbarTime.Year, kbarTime.Month, kbarTime.Day, kbarTime.Hour, kbarTime.Minute, 0);

                                    // If the data is still within the same minute, add it to the buffer
                                    if (kbarTime >= currentMinute && kbarTime < currentMinute.AddMinutes(1))
                                    {
                                        kbarBuffer.Add(kbar);
                                    }
                                    else
                                    {
                                        // Minute has changed, so create and log the candle
                                        if (kbarBuffer.Count > 0)
                                        {
                                            var open = kbarBuffer.First().o;
                                            var close = kbarBuffer.Last().c;
                                            var high = kbarBuffer.Max(k => k.h);
                                            var low = kbarBuffer.Min(k => k.l);

                                            Logger.Log($"[CANDLE] Time: {currentMinute:HH:mm}, O: {open}, H: {high}, L: {low}, C: {close}", Color.Yellow);

                                            SaveCandleToCsv(currentMinute, open, high, low, close);
                                        }

                                        // Clear the buffer for the new minute
                                        kbarBuffer.Clear();
                                        currentMinute = new DateTime(kbarTime.Year, kbarTime.Month, kbarTime.Day, kbarTime.Hour, kbarTime.Minute, 0);
                                        kbarBuffer.Add(kbar);
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

        // Save candle to CSV file safely using file lock
        private static void SaveCandleToCsv(DateTime time, double open, double high, double low, double close)
        {
            lock (FileLock)
            {
                bool fileExists = File.Exists(CsvFilePath);

                using (var writer = new StreamWriter(CsvFilePath, append: true))
                {
                    if (!fileExists)
                    {
                        writer.WriteLine("Time,Open,High,Low,Close");
                    }

                    writer.WriteLine($"{time:yyyy-MM-dd HH:mm},{open},{high},{low},{close}");
                }
            }
        }
    }
} // namespace end
