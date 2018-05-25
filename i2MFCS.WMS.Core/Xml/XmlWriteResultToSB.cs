using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace i2MFCS.WMS.Core.Xml
{
    public class XmlWriteResultToSB : XmlBasicToERP
    {
        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\WMSWriteResultToSB.xsd";

        public int? ERPID { get; set; }
        public int OrderID { get; set; }

        public XmlWriteResultToSB() : base(_DeffileNameSchema)
        {
        }

        public override string Reference()
        {
            string erpidstr = ERPID == null ? "" : ERPID.ToString();
            return $"{nameof(XmlWriteResultToSB)}({erpidstr}, {OrderID})";
        }

        public override string BuildXml()
        {
            using (var dc = new WMSContext())
            {
                var erpcmd = dc.CommandERP.FirstOrDefault(p => p.ID == ERPID);
                int reference = erpcmd != null ? erpcmd.ERP_ID : 0;

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
                                                                .Sum(p => p != null? p.TU.Qty: 0)
                                    }).ToList();

                XElement el0 = null;

                LoadSchema();
                XDocument XDocument = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                                    el0 = new XElement("Results"));
                XNamespace ns = XDocument.Root.Name.Namespace;

                foreach (var o in orders)
                {
                    el0.Add(new XElement("Detail"));
                    XElement el1 = (el0.LastNode as XElement);

                    el1.Add(new XElement("ErpId", XmlConvert.ToString(reference)));
                    if (o.Required == o.Delivered)
                    {
                        el1.Add(new XElement("Status", 0));
                        el1.Add(new XElement("ResultString", $"DELIVERED: {o.Delivered}/{o.Required}"));
                    }
                    else
                    {
                        el1.Add(new XElement("Status", 1));
                        el1.Add(new XElement("ResultString", $"CANCELED: {o.Required - o.Delivered}/{o.Required}"));
                    }
                    el1.Add(new XElement("SKUID", o.Key.SKU_ID));
                    el1.Add(new XElement("Batch", o.Key.SKU_Batch));
                    el1.Add(new XElement("Quantity", o.Delivered));
                }

                return XDocument.ToString();

            }
        }
    }
}
