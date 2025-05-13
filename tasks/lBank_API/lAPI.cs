using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using Newtonsoft.Json;

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

            var receiveBuffer = new byte[1024];
            while (true)
            {
                var result = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                var message = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                Console.WriteLine($"Received message: {message}");
            }
        }
    }

    static async Task Main(string[] args)
    {
        await ListenToLBank();
    }
}
