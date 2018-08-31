﻿using System;
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
        public enum OrderStatus { NotActive = 0, Active, ReadyForOperator, OperatorFinished, OnTargetPart, OnTargetAll, Canceled, Finished }
        public enum OrderType { Store = 0, Retrieve }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        public string SKU_Package { get; set; }
        [Required]
        public double SKU_Qty { get; set; }
        [Required, MaxLength(15), ForeignKey("FK_Destination")]
        public string Destination { get; set; }
        [Required]
        public DateTime ReleaseTime { get; set; }
        [Required]
        public OrderType Type { get; set; }
        [Required]
        public OrderStatus Status { get; set; }

        public virtual CommandERP FK_CommandERP { get; set; }
        public virtual TU_ID FK_TU_ID { get; set; }
        public virtual SKU_ID FK_SKU_ID { get; set; }
        public virtual Package_ID FK_Package_ID { get; set; }
        public virtual PlaceID FK_Destination { get; set; }
        [InverseProperty("FK_OrderID")]
        public virtual List<Command> FK_Commands { get; set; }
        public override string ToString()
        {
            return $"({ID},{ERP_ID},{SKU_Batch},{Status}) {SKU_ID}x{SKU_Qty}->{Destination}";
        }

    }
}
