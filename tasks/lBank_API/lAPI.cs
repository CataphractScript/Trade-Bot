using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

class Program
{
    static async Task ListenToLBank()
    {
        var uri = new Uri("wss://www.lbkex.net/ws/V2/");

        // Outer loop to reconnect automatically on disconnection or error
        while (true)
        {
            using (var websocket = new ClientWebSocket())
            {
                try
                {
                    Console.WriteLine("Connecting to WebSocket...");
                    await websocket.ConnectAsync(uri, CancellationToken.None);
                    Console.WriteLine("Connected to WebSocket.");

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
                    Console.WriteLine("Subscription request sent.");

                    var receiveBuffer = new byte[4096];

                    // Listen to messages while connection is open
                    while (websocket.State == WebSocketState.Open)
                    {
                        var result = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                        // If server sends a close message, close the connection gracefully
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("Server initiated close. Closing WebSocket...");
                            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing as requested by server", CancellationToken.None);
                            break;
                        }

                        // Convert received bytes to string
                        var message = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                        Console.WriteLine($"Received message: {message}");

                        try
                        {
                            // Deserialize message into a dictionary
                            var parsedMessage = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);

                            // Check if message is of type 'kbar' and contains all necessary fields
                            if (parsedMessage != null &&
                                parsedMessage.ContainsKey("type") &&
                                parsedMessage["type"]?.ToString() == "kbar" &&
                                parsedMessage.ContainsKey("kbar") &&
                                parsedMessage.ContainsKey("pair") &&
                                parsedMessage.ContainsKey("SERVER") &&
                                parsedMessage.ContainsKey("TS"))
                            {
                                // Extract and store the desired structure
                                var messageData = new
                                {
                                    kbar = parsedMessage["kbar"],
                                    type = parsedMessage["type"],
                                    pair = parsedMessage["pair"],
                                    SERVER = parsedMessage["SERVER"],
                                    TS = parsedMessage["TS"]
                                };

                                // Save the message to file immediately
                                SaveSingleMessageToFile(messageData, "lbank_data.json");
                            }
                            else
                            {
                                Console.WriteLine("Ignored non-kbar message.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing message: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebSocket error: {ex.Message}");
                }
            }

            // Wait a few seconds before trying to reconnect
            Console.WriteLine("Reconnecting in 5 seconds...");
            await Task.Delay(5000);
        }
    }

    // Appends a single message to a JSON file (creates file if needed)
    static void SaveSingleMessageToFile(object message, string filePath)
    {
        try
        {
            List<object> existingMessages = new List<object>();

            // Load existing data if file exists
            if (File.Exists(filePath))
            {
                var existingJson = File.ReadAllText(filePath);
                existingMessages = JsonConvert.DeserializeObject<List<object>>(existingJson) ?? new List<object>();
            }

            // Add new message to list
            existingMessages.Add(message);

            // Save updated list back to file
            var jsonToWrite = JsonConvert.SerializeObject(existingMessages, Formatting.Indented);
            File.WriteAllText(filePath, jsonToWrite);
            Console.WriteLine("Message saved to file.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file: {ex.Message}");
        }
    }

    // Entry point of the application
    static async Task Main(string[] args)
    {
        await ListenToLBank();
    }
}
