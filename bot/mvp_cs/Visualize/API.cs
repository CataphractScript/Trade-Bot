using CsvHelper;
using CsvHelper.Configuration;
using LiveChartsCore.SkiaSharpView.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Visualize
{
    internal static class API
    {
        private static KbarData? lastKbar = null;

        //private static List<KbarData> kbarBuffer = new List<KbarData>();
        private static DateTime currentMinute = DateTime.MinValue;
        private static readonly string CsvFilePath = "candles.csv";
        private static readonly object FileLock = new object(); // for thread-safe file access

        public static async Task RestApiAsync(string symbol = "btc_usdt", string timeFrame = "minute1", int size = 100)
        {
            Uri HttpUrl = new Uri("https://api.lbkex.com/v2/kline.do");

            // Calculate the Unix timestamp for the start time of requested candles
            long timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (60 * size);

            // Prepare the query parameters for the API request
            var parameters = new Dictionary<string, string>()
            {
                { "symbol", symbol },
                { "size", size.ToString() },
                { "type", timeFrame },
                { "time", timeStamp.ToString() }
            };

            //using var client = new HttpClient();
            using (HttpClient client = new HttpClient())
            {

                try
                {
                    // Build the full request URI with query parameters
                    var uriBuilder = new UriBuilder(HttpUrl);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    foreach (var param in parameters)
                        query[param.Key] = param.Value;
                    uriBuilder.Query = query.ToString();

                    // Send GET request to the API
                    var response = await client.GetAsync(uriBuilder.Uri);
                    response.EnsureSuccessStatusCode(); // Throw if HTTP response is unsuccessful

                    // Read the response content as a JSON string
                    string json = await response.Content.ReadAsStringAsync();

                    // Deserialize JSON response to LBankApiResponse object
                    var apiResponse = JsonConvert.DeserializeObject<HttpApiResponse>(json);

                    Console.WriteLine("\nFull response from server: " + json);

                    // Check if API response indicates success
                    if (apiResponse.Result != "true")
                    {
                        Logger.Log("❌ API Error: " + apiResponse);
                        return;
                    }

                    // Loop through each candle data and save it to CSV
                    foreach (var item in apiResponse.Data)
                    {
                        // Convert Unix timestamp (seconds) to DateTime
                        var unixTimestamp = Convert.ToInt64(item[0]);
                        DateTime rawTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
                        DateTime dateTime = new DateTime(rawTime.Year, rawTime.Month, rawTime.Day, rawTime.Hour, rawTime.Minute, 0, DateTimeKind.Utc);

                        // Parse candle prices as double
                        double Open = Convert.ToDouble(item[1]);
                        double High = Convert.ToDouble(item[2]);
                        double Low = Convert.ToDouble(item[3]);
                        double Close = Convert.ToDouble(item[4]);

                        // Save candle data to CSV file
                        SaveCandleToCsv(dateTime, Open, High, Low, Close);
                    }

                    Logger.Log("Data saved to CSV successfully.");
                }
                catch (HttpRequestException e)
                {
                    Logger.Log($"Request error: {e.Message}");
                }
                catch (Exception e)
                {
                    Logger.Log($"Unexpected error: {e.Message}");
                }
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
                                var deserializeMessage = JsonConvert.DeserializeObject<ApiResponse>(message);

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



        //Save candle to CSV file safely using file lock
        //private static void SaveCandleToCsv(DateTime time, double open, double high, double low, double close)
        //{
        //    lock (FileLock)
        //    {
        //        bool fileExists = File.Exists(CsvFilePath);

        //        using (var writer = new StreamWriter(CsvFilePath, append: true))
        //        {
        //            if (!fileExists)
        //            {
        //                writer.WriteLine("Time,Open,High,Low,Close");
        //            }

        //            writer.WriteLine($"{time:yyyy-MM-dd HH:mm},{open},{high},{low},{close}");
        //        }
        //    }
        //}

        private static void SaveCandleToCsv(DateTime time, double open, double high, double low, double close)
        {
            lock (FileLock) // Ensure thread safety when writing to the file
            {
                bool fileExists = File.Exists(CsvFilePath);
                bool hasHeader = false;

                if (fileExists)
                {
                    // Read the first line to check if header exists
                    using (var reader = new StreamReader(CsvFilePath))
                    {
                        var firstLine = reader.ReadLine();
                        hasHeader = firstLine != null &&
                                    firstLine.Contains("Date") &&
                                    firstLine.Contains("Open") &&
                                    firstLine.Contains("High") &&
                                    firstLine.Contains("Low") &&
                                    firstLine.Contains("Close");
                    }
                }

                // Configure CsvHelper settings
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = !fileExists || !hasHeader
                };

                // Open file in append mode if it exists, otherwise create a new file
                using (var stream = File.Open(CsvFilePath, fileExists ? FileMode.Append : FileMode.Create))
                using (var writer = new StreamWriter(stream))
                using (var csv = new CsvWriter(writer, config))
                {
                    // Write header if the file is new or header is missing
                    if (!fileExists || !hasHeader)
                    {
                        csv.WriteHeader<CandleModel>();
                        csv.NextRecord();
                    }

                    // Create and write the candle data
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
                }
            }
        }
    }
} // namespace end
