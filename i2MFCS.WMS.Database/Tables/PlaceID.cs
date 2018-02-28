using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class PlaceID
    {
        [Key, MaxLength(30)]    
        public string ID { get; set; }
        [Required]
        public int AccessCost { get; set; }
        [Required]
        public int DimensionClass { get; set; }
        [Required]
        public int FrequencyClass { get; set; }
        [Required]
        public int Status { get; set; }

        [InverseProperty("FK_PlaceID")]
        public virtual List<Place> FK_Places {get;set;}
        [InverseProperty("FK_Source")]
        public virtual List<Command> FK_Source_Commands { get; set; }
        [InverseProperty("FK_Target")]
        public virtual List<Command> FK_Target_Commands { get; set; }
        [InverseProperty("FK_Destination")]
        public virtual List<Order> FK_Orders { get; set; }
        public override string ToString()
        {
            return $"({ID},{AccessCost},{DimensionClass},{FrequencyClass},{Status})";
        }
    }
}
