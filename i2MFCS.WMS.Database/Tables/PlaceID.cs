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
        public int Size { get; set; }
        public int Status { get; set; }

        public virtual List<Place> FK_Place {get;set;}
    }
}
