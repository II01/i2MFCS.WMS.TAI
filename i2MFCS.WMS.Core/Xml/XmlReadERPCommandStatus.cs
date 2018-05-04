using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace i2MFCS.WMS.Core.Xml
{
    public class XmlReadERPCommandStatus : XmlBasicToERP
    {
        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\ERPCommandReply.xsd";
        public IEnumerable<Order> OrderToReport { get; set; }

        public XmlReadERPCommandStatus() : base(_DeffileNameSchema)
        { }

        public override string Reference()
        {
            return $"{nameof(XmlReadERPCommandStatus)}";
        }

        public override string BuildXml()
        {
            XElement el0 = null;
            XDocument xmlOut = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                             el0 = new XElement("ERPCommandReply"));

            el0.Add(new XElement("CommandStatus"));
            XElement el1 = el0.LastNode as XElement;
            foreach (Order o in OrderToReport)
            {
                el1.Add(new XElement("Command"));
                XElement el2 = el1.LastNode as XElement;
                el2.Add(new XElement("ERPID", o.ERP_ID));
                el2.Add(new XElement("OrderID", o.OrderID));
                el2.Add(new XElement("SuborderID", o.SubOrderID));
                el2.Add(new XElement("SuborderERPID", o.SubOrderERPID));
                el2.Add(new XElement("Status", o.Status));
                el2.Add(new XElement("Details", "No details..."));
            }
            return xmlOut.ToString();
        }
    }
}
