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
        [Required]
        public int TU_ID { get; set; }
        [MaxLength(15),Required]
        public string Source { get; set; }
        [MaxLength(15),Required]
        public string Target { get; set; }
        [Required]
        public int Status { get; set; }

        [Required, ForeignKey("TU_ID")]
        public virtual TU_ID FK_TU_ID { get; set; }
    }
}
