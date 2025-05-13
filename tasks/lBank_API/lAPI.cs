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
        using (var websocket = new ClientWebSocket())
        {
            await websocket.ConnectAsync(uri, CancellationToken.None);
            Console.WriteLine("Connected to WebSocket.");

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
            Console.WriteLine("Request sent to WebSocket.");

            var receiveBuffer = new byte[4096];
            var messages = new List<string>();

            while (true)
            {
                var result = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                var message = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                Console.WriteLine($"Received message: {message}");

                messages.Add(message);

                // Save every 10 messages to the file (you can change this threshold as needed)
                if (messages.Count >= 10)
                {
                    SaveMessagesToFile(messages, "lbank_data.json");
                    messages.Clear();
                }
            }
        }
    }

    static void SaveMessagesToFile(List<string> messages, string filePath)
    {
        try
        {
            // If the file exists, load the previous data
            List<string> existingMessages = new List<string>();
            if (File.Exists(filePath))
            {
                var existingJson = File.ReadAllText(filePath);
                existingMessages = JsonConvert.DeserializeObject<List<string>>(existingJson) ?? new List<string>();
            }

            // Add new messages
            existingMessages.AddRange(messages);

            // Write to the file
            var jsonToWrite = JsonConvert.SerializeObject(existingMessages, Formatting.Indented);
            File.WriteAllText(filePath, jsonToWrite);
            Console.WriteLine($"Saved {messages.Count} messages to {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to file: {ex.Message}");
        }
    }

    static async Task Main(string[] args)
    {
        await ListenToLBank();
    }
}
