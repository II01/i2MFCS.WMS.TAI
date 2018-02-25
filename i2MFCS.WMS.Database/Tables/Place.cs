using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class Place
    {
        [Key, ForeignKey("FK_PlaceID"), MaxLength(30), Column(Order=0)]
        public string PlaceID { get; set; }
        [Key, ForeignKey("FK_TU_ID"), Column(Order = 1)]
        public int TU_ID { get; set; }
        [Timestamp]
        public byte[] TimeStamp { get; set; }

        public virtual TU_ID FK_TU_ID { get; set; }
        public virtual PlaceID FK_PlaceID { get; set; }
    }
}
