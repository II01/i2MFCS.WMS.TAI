using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class CommandERP
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [Required]
        public int ERP_ID { get; set; }
        [Required]
        public string Command { get; set; }
        [Required]
        public string Reference { get; set; }
        [Required]
        public int Status { get; set; }
        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Time { get; set; }
        [InverseProperty("FK_CommandERP")]
        public virtual List<Order> FK_Command { get; set; }
    }
}
