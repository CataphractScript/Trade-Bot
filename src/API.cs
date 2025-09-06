using CsvHelper;
using LiveChartsCore.SkiaSharpView.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;

namespace Visualize
{
    internal static class API
    {
        private static WsKbarData? lastKbar = null;

        //private static List<KbarData> kbarBuffer = new List<KbarData>();
        private static DateTime currentMinute = DateTime.MinValue;
        private static readonly string CsvFilePath = "candles.csv";
        private static readonly object FileLock = new object(); // for thread-safe file access

        public static async Task RestApiAsync(string symbol = "btc_usdt", string timeFrame = "minute1", int size = 100)
        {
            try
            {
                Uri HttpUrl = new Uri("https://api.lbkex.com/v2/kline.do");
                //string url = "https://api.lbkex.com/v2/kline.do";

                // Calculate the starting time based on the number of candles and interval
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (60 * size);

                // Prepare query parameters
                var parameters = new Dictionary<string, string>
                {
                    { "symbol", symbol },
                    { "size", size.ToString() },
                    { "type", timeFrame },
                    { "time", timestamp.ToString() }
                };

                // Build the full URL with query string
                var uriBuilder = new UriBuilder(HttpUrl);
                var query = new FormUrlEncodedContent(parameters);
                uriBuilder.Query = await query.ReadAsStringAsync();


                using HttpClient httpClient = new HttpClient();
                // Send HTTP GET request to the API
                HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.ToString());
                response.EnsureSuccessStatusCode(); // Throw exception if the response is not successful

                // Read and parse the response body
                string content = await response.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);

                Logger.Log("\nFull response from server: " + json);

                // Check for API-level error
                if (json["result"]?.ToString() != "true")
                {
                    Logger.Log("❌ API Error: " + json["msg"]?.ToString());
                    return;
                }

                // Parse the data array from JSON
                var data = json["data"] as JArray;
                var candles = new List<HttpData>();

                // Convert each item into a CandleData object
                foreach (var item in data)
                {
                    candles.Add(new HttpData
                    {
                        Timestamp = DateTimeOffset.FromUnixTimeSeconds(item[0].Value<long>()).UtcDateTime.AddHours(8),
                        Open = item[1].Value<double>(),
                        High = item[2].Value<double>(),
                        Low = item[3].Value<double>(),
                        Close = item[4].Value<double>(),
                        Volume = item[5].Value<double>()
                    });
                }

                // Write the candles to a CSV file
                foreach (var candle in candles)
                {
                    SaveCandleToCsv(candle.Timestamp, candle.Open, candle.High, candle.Low, candle.Close);
                }

                Logger.Log("✅ Candlestick data saved to: " + CsvFilePath);
            }
            catch (HttpRequestException e)
            {
                // Handle connection errors
                Logger.Log($"Connection error: {e.Message}");
            }
        }

        public static async Task WebsocketAPI(CartesianChart Chart)
        {
            Uri ServerUri = new Uri("wss://www.lbkex.net/ws/V2/");

            // Keep the WebSocket connection alive by reconnecting on disconnect
            while (true)
            {
                using (ClientWebSocket websocket = new ClientWebSocket())
                {
                    try
                    {
                        Logger.Log("Connecting to WebSocket...");

                        // Connect to the WebSocket server asynchronously
                        await websocket.ConnectAsync(ServerUri, CancellationToken.None);

                        Logger.Log("Connected to WebSocket.");

                        // Create a subscription request message in the expected JSON format
                        var requestMessage = new
                        {
                            action = "subscribe",
                            subscribe = "kbar",    // Subscribe to candlestick data
                            kbar = "1min",         // 1-minute interval candles
                            pair = "btc_usdt"      // Trading pair: Bitcoin / USDT
                        };

                        // Serialize the request message to JSON string
                        var jsonMessage = JsonConvert.SerializeObject(requestMessage);

                        // Convert JSON string to byte array for sending via WebSocket
                        var buffer = Encoding.UTF8.GetBytes(jsonMessage);

                        // Send the subscription message to the WebSocket server
                        await websocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                        Logger.Log("Subscription request sent.");

                        // Buffer to receive incoming WebSocket messages
                        var receiveBuffer = new byte[4096];

                        // Loop while WebSocket connection is open to receive messages
                        while (websocket.State == WebSocketState.Open)
                        {
                            // StringBuilder to accumulate message fragments if the message is split
                            var fullMessage = new StringBuilder();

                            WebSocketReceiveResult result;

                            do
                            {
                                // Receive a chunk of the WebSocket message asynchronously
                                result = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                                // Decode the received bytes to string
                                string partialMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                                // Append the fragment to the full message
                                fullMessage.Append(partialMessage);

                                // If server requests to close the connection, respond accordingly
                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    Logger.Log("Server initiated close. Closing WebSocket...", Color.Red);

                                    // Close the WebSocket connection gracefully
                                    await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", CancellationToken.None);
                                    break;
                                }

                            } while (!result.EndOfMessage); // Continue until the entire message is received

                            // Convert the complete message to string
                            string message = fullMessage.ToString();

                            Logger.Log($"Received message: {message}", Color.Aqua);

                            // Handle Ping messages from server to keep connection alive
                            if (message.Contains("\"action\":\"ping\""))
                            {
                                var json = JObject.Parse(message);
                                string? pingId = json["ping"]?.ToString();

                                if (!string.IsNullOrEmpty(pingId))
                                {
                                    // Respond with Pong message carrying the ping ID
                                    var pongMessage = new { action = "pong", pong = pingId };
                                    string pongJson = JsonConvert.SerializeObject(pongMessage);
                                    byte[] pongBuffer = Encoding.UTF8.GetBytes(pongJson);

                                    await websocket.SendAsync(new ArraySegment<byte>(pongBuffer), WebSocketMessageType.Text, true, CancellationToken.None);

                                    Logger.Log($"Pong sent for ping {pingId}", Color.Green);

                                    continue; // Skip further processing of this ping message
                                }
                            }

                            try
                            {
                                // Deserialize the JSON message into ApiResponse object
                                var deserializeMessage = JsonConvert.DeserializeObject<WsApiResponse>(message);

                                // Check if the message contains valid candlestick data
                                if (deserializeMessage != null &&
                                    deserializeMessage.type == "kbar" &&
                                    deserializeMessage.kbar != null &&
                                    !string.IsNullOrEmpty(deserializeMessage.pair) &&
                                    !string.IsNullOrEmpty(deserializeMessage.SERVER) &&
                                    deserializeMessage.TS != DateTime.MinValue)
                                {
                                    var kbar = deserializeMessage.kbar;

                                    // Normalize the time to the minute (ignore seconds)
                                    DateTime kbarTime = new DateTime(kbar.t.Year, kbar.t.Month, kbar.t.Day, kbar.t.Hour, kbar.t.Minute, 0, DateTimeKind.Utc);

                                    // If a new minute has started, save the previous candle
                                    if (kbarTime > currentMinute)
                                    {
                                        if (lastKbar != null)
                                        {
                                            // Log candle details
                                            Logger.Log($"[CANDLE] Date: {currentMinute:HH:mm}, O: {lastKbar.o}, H: {lastKbar.h}, L: {lastKbar.l}, C: {lastKbar.c}", Color.Yellow);

                                            // Save the candle data to CSV file
                                            SaveCandleToCsv(currentMinute, lastKbar.o, lastKbar.h, lastKbar.l, lastKbar.c);

                                            // Add the candle to the chart UI
                                            ChartHelper.AddCandle(Chart, currentMinute, lastKbar.o, lastKbar.h, lastKbar.l, lastKbar.c);
                                        }

                                        // Update the current minute marker to the new minute
                                        currentMinute = kbarTime;
                                    }

                                    // Update the last candle data for the current minute
                                    lastKbar = kbar;

                                    // (Commented out code here is an alternative way of buffering candles per minute)

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
                                else
                                {
                                    // Log if the message is not a valid candle update
                                    Logger.Log("Ignored non-kbar message.", Color.Green);
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log any errors occurred during message processing
                                Logger.Log($"Error processing message: {ex.Message}", Color.Red);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log errors related to WebSocket connection or sending/receiving messages
                        Logger.Log($"WebSocket error: {ex.Message}", Color.Red);
                    }
                }

                // Wait 5 seconds before attempting to reconnect after a disconnect or error
                Logger.Log("Reconnecting in 5 seconds...");
                await Task.Delay(5000);
            }
        }


        //    Save candle to CSV file safely using file lock
        //private static void SaveCandleToCsv(DateTime time, double open, double high, double low, double close)
        //{
        //    lock (FileLock)
        //    {
        //        bool fileExists = File.Exists(CsvFilePath);

        //        using (var writer = new StreamWriter(CsvFilePath, append: true))
        //        {
        //            //if (!fileExists)
        //            //{
        //            //    writer.WriteLine("Time,Open,High,Low,Close");
        //            //}

        //            writer.WriteLine($"{time:yyyy-MM-dd HH:mm},{open},{high},{low},{close}");
        //        }
        //    }
        //}


        //private static void SaveCandleToCsv(DateTime time, double open, double high, double low, double close)
        //{
        //    lock (FileLock)
        //    {
        //        bool fileExists = File.Exists(CsvFilePath);
        //        bool hasHeader = false;

        //        // Check if file exists and contains a valid header
        //        if (fileExists)
        //        {
        //            using (var reader = new StreamReader(CsvFilePath))
        //            {
        //                var firstLine = reader.ReadLine();
        //                hasHeader = !string.IsNullOrWhiteSpace(firstLine) &&
        //                            firstLine.Contains("Time") &&
        //                            firstLine.Contains("Open") &&
        //                            firstLine.Contains("High") &&
        //                            firstLine.Contains("Low") &&
        //                            firstLine.Contains("Close");
        //            }
        //        }

        //        // Write header if file doesn't exist or header is missing
        //        if (!fileExists || !hasHeader)
        //        {
        //            using (var headerWriter = new StreamWriter(CsvFilePath, append: false))
        //            {
        //                headerWriter.WriteLine("Time,Open,High,Low,Close");
        //                headerWriter.Flush();
        //            }
        //        }

        //        // Append the new candle record to the file
        //        using (var writer = new StreamWriter(CsvFilePath, append: true))
        //        {
        //            writer.WriteLine($"{time:yyyy-MM-dd HH:mm},{open},{high},{low},{close}");
        //            writer.Flush();
        //        }
        //    }
        //}

        private static void SaveCandleToCsv(DateTime time, double open, double high, double low, double close)
        {
            lock (FileLock)
            {
                bool fileExists = File.Exists(CsvFilePath);
                bool hasContent = false;
                bool hasHeader = false;

                // Check if file exists and whether it has content and header
                if (fileExists)
                {
                    using (var reader = new StreamReader(CsvFilePath))
                    {
                        var firstLine = reader.ReadLine();
                        hasContent = !string.IsNullOrWhiteSpace(firstLine);

                        if (hasContent)
                        {
                            hasHeader = firstLine.Contains("Date") &&
                                        firstLine.Contains("Open") &&
                                        firstLine.Contains("High") &&
                                        firstLine.Contains("Low") &&
                                        firstLine.Contains("Close");
                        }
                    }
                }

                // Write header if file doesn't exist or is empty or has no header
                if (!fileExists || !hasContent || !hasHeader)
                {
                    // Open file, write header, and close immediately
                    using (var stream = new FileStream(CsvFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteHeader<CandleModel>();
                        csv.NextRecord();
                        writer.Flush();
                    }
                }

                // Write candle record (append mode)
                using (var stream = new FileStream(CsvFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    var candle = new CandleModel
                    {
                        Date = time,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close
                    };

                    csv.WriteRecord(candle);
                    csv.NextRecord();
                    writer.Flush();
                }
            }
        }

    }
} // namespace end
