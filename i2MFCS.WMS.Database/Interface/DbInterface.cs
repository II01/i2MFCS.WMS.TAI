using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Interface
{
    public class DbInterface 
    {
        public void CreateDatabase()
        {
            using (WMSContext dc = new WMSContext())
            {
                dc.Database.Delete();
                dc.Database.Create();
            }
        }

        public void CreateInputCommands(int barcode)
        {
            using (var dc = new WMSContext())
            {

            }
        }

    }
}
