using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static i2MFCS.WMS.Database.Tables.Command;

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
        public string Box_ID { get; set; }
        [DataMember]
        public string Source { get; set; }
        [DataMember]
        public string Target { get; set; }
        [DataMember]
        public CommandOperation Operation { get; set; }
        [DataMember]
        public DateTime LastChange { get; set; }
        [DataMember]
        public Command.CommandStatus Status { get; set; }

        public override string ToString()
        {
            return $"({ID},{Order_ID ?? 0}):{TU_ID:d9}:{Source}->{Target}";
        }
    }
}
