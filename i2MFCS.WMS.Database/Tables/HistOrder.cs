using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class HistOrder
    {
        public enum HistOrderStatus {NotActive=0, Active,OnTargetPart, OnTargetAll, Canceled, Finished}

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        [ForeignKey("FK_CommandERP")]
        public int? ERP_ID { get; set; }
        [Required]
        public int OrderID { get; set; }
        public int SubOrderID { get; set; }
        public int SubOrderERPID {get; set;}
        [Required, MaxLength(15), ForeignKey("FK_SKU_ID")]
        public string SKU_ID { get; set; }
        [Required, MaxLength(200)]
        public string SubOrderName { get; set; }
        [Required]
        public double SKU_Qty { get; set; }
        [Required, MaxLength(15), ForeignKey("FK_Destination")]
        public string Destination { get; set; }
        [Required]
        public DateTime ReleaseTime { get; set; }
        [Required, MaxLength(50)]
        public string SKU_Batch { get; set; }
        [Required]
        public HistOrderStatus Status { get; set; }

        public virtual CommandERP FK_CommandERP { get; set; }
        public virtual SKU_ID FK_SKU_ID { get; set; }
        public virtual PlaceID FK_Destination { get; set; }
        public virtual List<HistCommand> FK_HistCommands { get; set; }
        public override string ToString()
        {
            return $"({ID},{ERP_ID},{SKU_Batch},{Status}) {SKU_ID}x{SKU_Qty}->{Destination}";
        }

    }
}
