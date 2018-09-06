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
        public enum HistOrderStatus { Disabled = 0, NotActive, Active, OnTargetPart, OnTargetAll, Canceled, Finished }
        public enum HistOrderOperation { None = 0, StoreTray, MoveTray, DropBox, PickBox, RetrieveTray, Confirm }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        [ForeignKey("FK_CommandERP")]
        public int? ERP_ID { get; set; }
        [Required]
        public int OrderID { get; set; }
        public int SubOrderID { get; set; }
        public int SubOrderERPID { get; set; }
        [Required, MaxLength(200)]
        public string SubOrderName { get; set; }
        [Required, ForeignKey("FK_TU_ID")]
        public int TU_ID { get; set; }
        [Required, MaxLength(30), ForeignKey("FK_SKU_ID")]
        public string SKU_ID { get; set; }
        [Required, MaxLength(50)]
        public string SKU_Batch { get; set; }
        [Required, MaxLength(30)]
        public string Box_ID { get; set; }
        [Required]
        public double SKU_Qty { get; set; }
        [Required, MaxLength(15), ForeignKey("FK_Destination")]
        public string Destination { get; set; }
        [Required]
        public DateTime ReleaseTime { get; set; }
        [Required]
        public HistOrderOperation Operation { get; set; }
        [Required]
        public HistOrderStatus Status { get; set; }

        public virtual CommandERP FK_CommandERP { get; set; }
        public virtual TU_ID FK_TU_ID { get; set; }
        public virtual SKU_ID FK_SKU_ID { get; set; }
        public virtual Box_ID FK_Box_ID { get; set; }
        public virtual PlaceID FK_Destination { get; set; }
        [InverseProperty("FK_HistOrderID")]
        public virtual List<HistCommand> FK_HistCommands { get; set; }
        public override string ToString()
        {
            return $"({ID}, {ERP_ID ?? 0}+{OrderID}): {Operation.ToString()}, ({TU_ID}, {Box_ID}, {SKU_ID}, {SKU_Batch}) -> {Destination}, {Status})";
        }

    }
}
