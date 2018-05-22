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
        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\WMSWriteResultsToSB.xsd";

        public int ERPID { get; set; }
        public IEnumerable<OrderLineReport> OrderLineReport { get; set; }


        public XmlWriteResultToSB() : base(_DeffileNameSchema)
        {
        }

        public override string Reference()
        {
            return $"{nameof(XmlWriteResultToSB)}({string.Join(", ", OrderLineReport)})";
        }

        public override string BuildXml()
        {

            XElement el0 = null;

            LoadSchema();
            XDocument XDocument = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                                el0 = new XElement("Results"));
            XNamespace ns = XDocument.Root.Name.Namespace;

            foreach(var olr in OrderLineReport)
            {
                el0.Add(new XElement("Detail"));
                XElement el1 = (el0.LastNode as XElement);

                el1.Add(new XElement("ErpId", XmlConvert.ToString(olr.ERPID)));
                el1.Add(new XElement("Status", olr.Status));
                el1.Add(new XElement("ResultString", olr.ResultString));
                el1.Add(new XElement("SKUID", olr.SKUID));
                el1.Add(new XElement("Batch", olr.Batch));
                el1.Add(new XElement("Quantity", olr.Quantity));
            }

            return XDocument.ToString();
        }
    }
}
