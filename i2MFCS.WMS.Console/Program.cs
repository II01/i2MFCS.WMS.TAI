using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using i2MFCS.WMS.Console;
using i2MFCS.WMS.Core.Xml;
using i2MFCS.WMS.Database.Interface;
using i2MFCS.WMS.Database.Tables;
using i2MFCS.WMS.WCF;

namespace i2MFCS.WMS.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {

                /* TEST XmlReadERPCommandStatus
                var erpCommand = new XmlReadERPCommandStatus();
                using (var dc = new WMSContext())
                    erpCommand.OrderToReport = (from o in dc.Orders
                                                select o).ToList();
                File.WriteAllText(@"..\\..\\test1.xml", erpCommand.BuildXml());
                */



                /* TEST XmlReadERPCommand
                var erpCommand = new XmlReadERPCommand();
                erpCommand.ProcessXml(File.ReadAllText(@"..\..\..\i2MFCS.WMS.Core\Xml\ERPCommand.xml"));
                */

                /* TEST InputCommand
                Model model = new Model();
                model.CreateInputCommand("T014");
                */

                /* Test WmsToERPCommands
                WMSWriteMovementToSBWithBarcode cmd = new WMSWriteMovementToSBWithBarcode
                {
                    DocumentID = 1,
                    DocumentType = "Type1",
                    SKUID = "MAT01",
                    TU_IDs = new List<int> { 100,101}
                };
                File.WriteAllText(@"..\\..\\test1.xml", cmd.BuildXml());
                */
                WMSWritePickToDocument cmd = new WMSWritePickToDocument
                {
                    DocumentID = 1,
                    Orders = new List<Order> {
                        new Order
                        {
                            Destination = "W:21:108:1:1",
                            ERP_ID = 9,
                            ID = 1,
                            OrderID = 2,
                            SubOrderID = 3,
                            SubOrderName = "Cust1",
                            SKU_Qty = 10,
                            SKU_Batch = "Batch1",
                            SKU_ID = "MAT01",
                            ReleaseTime = DateTime.Now,
                            Status = 0
                        },
                        new Order
                        {
                            Destination = "W:21:108:1:2",
                            ERP_ID = 10,
                            ID = 2,
                            OrderID = 3,
                            SubOrderID = 4,
                            SubOrderName = "Cust2",
                            SKU_Qty = 10,
                            SKU_Batch = "Batch2",
                            SKU_ID = "MAT02",
                            ReleaseTime = DateTime.Now,
                            Status = 0
                        }
                  }
                };
                File.WriteAllText(@"..\\..\\test1.xml", cmd.BuildXml());


                /* TEST OutputCommand
                Model model = new Model();
                model.CreateInputCommand("T014");
                System.Console.WriteLine($"Started at : {DateTime.Now}");
                model.CreateOutputCommands(9, 1, "W:32:01");
                System.Console.WriteLine($"Finished at : {DateTime.Now}");
                System.Console.ReadLine();

                /*
                using (var ERPHost = new ServiceHost(typeof(WMSToERP)))
                using (var MFCSHost = new ServiceHost(typeof(WMSToMFCS)))
                using (var UIHost = new ServiceHost(typeof(WMSToUI)))
                {
                    ERPHost.Open();
                    MFCSHost.Open();
                    UIHost.Open();

                    /// Testing functionity
                    Model dc = new Model();
                    dc.CreateDatabase();
                    dc.FillPlaceID();
                    dc.UpdateRackFrequencyClass(new double[] { 0.1, 0.2, 0.3 });

                    dc.CreateInputCommands("T014", 110, 0);
                    Debug.WriteLine("dc.CreateInputCommands(T14, 110, 1) finished");
                    dc.CreateOutputCommands(100, "", new List<string> { "W:22:001:2:1", "W:22:001:3:1", "W:22:001:4:1", "W:22:001:5:1" });
                    Debug.WriteLine("dc.CreateOutputCommands(100, 1, new List<string> { W:22:001:2:1, W:22:001:3:1, W:22:001:4:1, W:22:001:5:1 })");
                    System.Console.WriteLine($"WCF Service started...\n Press ENTER to stop.");
                    System.Console.ReadLine();
                }
                */

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
