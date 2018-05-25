using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestQuery
{
    class Program
    {
        static void Main(string[] args)
        {
            int ERPID = 31415;
            int OrderID = 0;

            using (var dc = new WMSContext())
            {
                var orders = dc.Orders
                                .Where(p => p.ERP_ID == ERPID && p.OrderID == OrderID)
                                .GroupBy(
                                    (by) => new { by.ID, by.SKU_ID, by.SKU_Batch },
                                    (key, grp) => new
                                    {
                                        Key = key,
                                        Required = grp.Sum(p => p.SKU_Qty),
                                        Delivered = dc.Commands.Where(p => p.Order_ID == key.ID && p.Status == Command.CommandStatus.Finished &&
                                                                        (p.Target.StartsWith("W:32") || p.Target.StartsWith("T04")))
                                                                .Join(dc.TUs,
                                                                    (cmd) => cmd.TU_ID,
                                                                    (tu) => tu.TU_ID,
                                                                    (cmd, tu) => new { TU = tu, Cmd = cmd })
                                                                .DefaultIfEmpty()
                                                                .Sum(p => p != null ? p.TU.Qty : 0)
                                    }).ToList();
                var erpcmd = dc.CommandERP.FirstOrDefault(p => p.ID == ERPID);
                int reference = erpcmd != null? erpcmd.ERP_ID : 0; 
            }
        }
    }
}
