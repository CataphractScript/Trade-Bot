using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main()
    {
        string symbol = "BTCUSDT";
        string url = $"https://api.coinex.com/v1/market/ticker?market={symbol}";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var response = await client.GetStringAsync(url);
                JObject json = JObject.Parse(response);

                if ((int)json["code"] == 0)
                {
                    var ticker = json["data"]["ticker"];
                    Console.WriteLine($"💰 {symbol} Price Info:");
                    Console.WriteLine("Last: " + ticker["last"]);
                    Console.WriteLine("High: " + ticker["high"]);
                    Console.WriteLine("Low: " + ticker["low"]);
                    Console.WriteLine("Volume: " + ticker["vol"]);
                }
                else
                {
                    Console.WriteLine("❌ Error: " + json["message"]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Exception: " + ex.Message);
            }
        }
    }
}
