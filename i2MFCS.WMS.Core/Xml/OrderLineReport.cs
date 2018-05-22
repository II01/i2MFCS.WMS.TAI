using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Core.Xml
{
    public class OrderLineReport
    {
        public int ERPID { get; set; }
        public int Status { get; set; }
        public string ResultString { get; set; }
        public string SKUID { get; set; }
        public string Batch { get; set; }
        public double Quantity { get; set; }

        public OrderLineReport()
        {
        }
        public override string ToString()
        {
            return $"({ERPID}, ({SKUID},{Batch})x{Quantity}, {Status}, {ResultString}";
        }
    }
}
