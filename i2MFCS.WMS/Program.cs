using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using i2MFCS.WMS.Database.Interface;
using i2MFCS.WMS.Database.Tables;
using i2MFCS.WMS.WCF;

namespace i2MFCS.WMS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                using (var ERPHost = new ServiceHost(typeof(WMSToERP)))
                using (var MFCSHost = new ServiceHost(typeof(WMSToMFCS)))
                using (var UIHost = new ServiceHost(typeof(WMSToUI)))
                {
                    ERPHost.Open();
                    MFCSHost.Open();
                    UIHost.Open();

                    /// Testing functionity
                    DbInterface dc = new DbInterface();
                    dc.CreateInputCommands("T014", 110, 0);
                    Debug.WriteLine("dc.CreateInputCommands(T14, 110, 1) finished");
                    dc.CreateOutputCommands(100, 1, new List<string> { "W:22:001:2:1", "W:22:001:3:1", "W:22:001:4:1", "W:22:001:5:1" });
                    Debug.WriteLine("dc.CreateOutputCommands(100, 1, new List<string> { W:22:001:2:1, W:22:001:3:1, W:22:001:4:1, W:22:001:5:1 })");
                    System.Console.WriteLine($"WCF Service started...\n Press ENTER to stop.");
                    System.Console.ReadLine();
                }


            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
