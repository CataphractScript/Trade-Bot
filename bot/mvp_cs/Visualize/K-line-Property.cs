using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visualize
{
    internal class WsKbarData
    {
        public double a { get; set; }
        public double c { get; set; }
        public DateTime t { get; set; }
        public double v { get; set; }
        public double h { get; set; }
        public string slot { get; set; }
        public double l { get; set; }
        public int n { get; set; }
        public double o { get; set; }
    }

    internal class WsApiResponse
    {
        public WsKbarData kbar { get; set; }
        public string type { get; set; }
        public string pair { get; set; }
        public string SERVER { get; set; }
        public DateTime TS { get; set; }
    }


    internal class HttpData
    {
        public DateTime Timestamp { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
    }

    //    internal class HttpApiResponse
    //    {
    //        [JsonProperty("msg")]
    //        public string Message { get; set; }

    //        [JsonProperty("result")]
    //        public string Result { get; set; }

    //        [JsonProperty("data")]
    //        public List<List<object>> Data { get; set; }

    //        [JsonProperty("error_code")]
    //        public int? ErrorCode { get; set; }

    //        [JsonProperty("ts")]
    //        public long? Timestamp { get; set; }
    //    }
}
