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
        [Key, MaxLength(30), Column(Order=0)]
        [ForeignKey("FK_PlaceID")]
        public string PlaceID { get; set; }
        [Key, Column(Order = 1)]
        [Required, ForeignKey("FK_TU_ID")]
        public int TU_ID { get; set; }
        [Timestamp]
        public byte[] TimeStamp { get; set; }
        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Time { get; set; }


        public virtual TU_ID FK_TU_ID { get; set; }

        public virtual PlaceID FK_PlaceID { get; set; }
        public override string ToString()
        {
            return $"({PlaceID},{TU_ID:d9})";
        }
    }
}
