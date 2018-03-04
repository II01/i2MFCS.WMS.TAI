using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.DTO
{
    [DataContract]
    public class DTOCommand
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int? Order_ID { get; set; }
        [DataMember]
        public int TU_ID { get; set; }
        [DataMember]
        public string Source { get; set; }
        [DataMember]
        public string Target { get; set; }
        [DataMember]
        public int Status { get; set; }

        public override string ToString()
        {
            return $"({ID},{Order_ID ?? 0}):{TU_ID}:{Source}->{Target}";
        }
    }
}
