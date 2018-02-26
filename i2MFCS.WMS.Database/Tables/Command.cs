using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class Command
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("FK_Order_ID")]
        public int Order_ID { get; set; }
        [Required,ForeignKey("FK_TU_ID")]
        public int TU_ID { get; set; }
        [Required,MaxLength(15), ForeignKey("FK_Source")]
        public string Source { get; set; }
        [Required,MaxLength(15), ForeignKey("FK_Target")]
        public string Target { get; set; }
        [Required]
        public int Status { get; set; }

        public virtual TU_ID FK_TU_ID { get; set; }
        public virtual PlaceID FK_Source { get; set; }
        public virtual PlaceID FK_Target { get; set; }
        public virtual Order FK_Order_ID { get; set; }

        public override string ToString()
        {
            return $"({ID},{Order_ID}):{TU_ID}:{Source}->{Target}";
        }
    }
}
