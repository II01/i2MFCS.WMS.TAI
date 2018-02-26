using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database
{

    public class SKU_ID
    {
        [Key]
        [MaxLength(30)]
        public string ID { get; set; }
        [MaxLength(50)]
        public string Description { get; set; }
        [Required]
        public double DefaultQty { get; set; }
        [MaxLength(10), Required]
        public string Unit { get; set; }
        [Required]
        public double Weight { get; set; }

    }
}
