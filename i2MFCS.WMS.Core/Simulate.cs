using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Core
{
    public class SimulateERP
    {
        public void SimulateIncomingTUs(string place, string skuid, string batch, double qty)
        {
            using (var dc = new WMSContext())
            {
                Place p = dc.Places.FirstOrDefault(prop => prop.PlaceID == place);
                if (p != null)
                {
                    TU tu = dc.TUs.FirstOrDefault(prop => prop.TU_ID == p.TU_ID);
                    if (tu == null)
                    {
                        dc.TUs.Add(tu = new TU
                        {
                            Batch = batch,
                            ExpDate = DateTime.Now,
                            ProdDate = DateTime.Now,
                            Qty = qty,
                            TU_ID = p.TU_ID,
                            SKU_ID = skuid
                        });
                        dc.SaveChanges();
                        Log.AddLog(Log.SeverityEnum.Event, nameof(SimulateIncomingTUs), $"Simulate new TU {tu.ToString()}");
                    }
                }
            }
        }
    }
}
