using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Core.DataExchange
{
    [DataContract]
    public class PlaceDiff
    {
        [DataMember]
        public int TUID { get; set; }
        [DataMember]
        public string PlaceWMS { get; set; }
        [DataMember]
        public string PlaceMFCS { get; set; }

        [DataMember]
        public DateTime? TimeWMS { get; set; }
        [DataMember]
        public DateTime? TimeMFCS { get; set; }

        public PlaceDiff()
        {
        }
    }
}
