using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class TU_ID
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }

        [Required]
        public int DimensionClass { get; set; }
        [Required]
        public int Blocked { get; set; }

        [InverseProperty("FK_TU_ID")]
        public virtual List<Command> FK_Command { get; set; }
        [InverseProperty("FK_TU_ID")]
        public virtual List<TU> FK_TU { get; set; }
        [InverseProperty("FK_TU_ID")]
        public virtual List<Place> FK_Place { get; set; }
        public override string ToString()
        {
            return $"({ID},{DimensionClass},{Blocked})";
        }


    }
}
