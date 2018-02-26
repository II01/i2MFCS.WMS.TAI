using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class Order
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public int ERP_ID { get; set; }
        [Required, ForeignKey("FK_SKU_ID")]
        public string SKU_ID { get; set; }
        [Required, ForeignKey("FK_Customer")]
        public int CustomerID { get; set; }
        [Required]
        public double Qty { get; set; }
        [Required, ForeignKey("FK_Destination")]
        public string Destination { get; set; }
        [Required]
        public int Sequence { get; set; }
        [Required]
        public int Status { get; set; }

        public virtual SKU_ID FK_SKU_ID { get; set; }
        public virtual Customer FK_Customer { get; set; }
        public virtual PlaceID FK_Destination { get; set; }
    }
}
