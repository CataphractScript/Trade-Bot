using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Visualize
{
    internal class KbarData
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

    internal class ApiResponse
    {
        public KbarData kbar { get; set; }
        public string type { get; set; }
        public string pair { get; set; }
        public string SERVER { get; set; }
        public DateTime TS { get; set; }
    }
}
