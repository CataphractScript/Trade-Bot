using System;
using System.Drawing.Text;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Visualize
{
    internal static class LBankWebSocket
    {
        private static readonly Uri ServerUri = new Uri("wss://www.lbkex.net/ws/V2/");

        // Loop to keep trying reconnect on disconnection
        public static async Task ListenToLBank()
        {

            int counter = 0;

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
                                var DeserializeMessage = JsonConvert.DeserializeObject<ApiResponse>(message);
                                counter++;

                                if (counter >= 20)
                                    counter = 0;
                                
                                /*
                                 * 
                                 * 
                                 * 
                                 */
                            }
                            catch (Exception ex)
                            {
                                Logger.Log("Ignored non-kbar message.", Color.Green);
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
    }
}
