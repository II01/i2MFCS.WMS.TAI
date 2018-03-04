using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class Parameter
    {
        [Key, MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(250)]
        public string Value { get; set; }
    }
}
