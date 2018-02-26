using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class Customer
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        [MaxLength(100)]
        public string Name { get; set; }

        [InverseProperty("FK_Customer")]
        public virtual List<Order> FK_Order { get; set; }

        public override string ToString()
        {
            return $"({ID}) {Name}";
        }
    }
}
