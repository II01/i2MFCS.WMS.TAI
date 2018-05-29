using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.DTO
{
    public class DTOOrder
    {
        public int ID { get; set; }
        public int? ERP_ID { get; set; }
        public int OrderID { get; set; }
        public int SubOrderID { get; set; }
        public int SubOrderERPID { get; set; }
        public string SKU_ID { get; set; }
        public string SubOrderName { get; set; }
        public double SKU_Qty { get; set; }
        public string Destination { get; set; }
        public DateTime ReleaseTime { get; set; }
        public string SKU_Batch { get; set; }
        public int Status { get; set; }
        public bool QualityControlOrder { get; set; }

        public DTOOrder()
        { }

        public DTOOrder(Order o)
        {
            Destination = o.Destination;
            ERP_ID = o.ERP_ID;
            ID = o.ID;
            OrderID = o.OrderID;
            SubOrderID = o.SubOrderID;
            SubOrderERPID = o.SubOrderERPID;
            SubOrderName = o.SubOrderName;
            ReleaseTime = o.ReleaseTime;
            SKU_Batch = o.SKU_Batch;
            SKU_ID = o.SKU_ID;
            SKU_Qty = o.SKU_Qty;
            QualityControlOrder = o.SubOrderID >= 1000;
        }

        public override string ToString()
        {
            return $"({ID},{ERP_ID??-1},{SKU_Batch},{Status}) {SKU_ID}x{SKU_Qty}->{Destination}";
        }

    }
}
