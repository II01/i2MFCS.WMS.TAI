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
    public class XmlWritePickToDocument : XmlBasicToERP
    {
        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\WMSWritePickToDocument.xsd";

        public int DocumentID { get; set; }
        public IEnumerable<Command> Commands { get; set; }


        public XmlWritePickToDocument() : base(_DeffileNameSchema)
        {
        }

        public override string Reference()
        {
            return $"{nameof(XmlWritePickToDocument)}";
        }

        public override string BuildXml()
        {
            XElement el0 = null;

            LoadSchema();
            XDocument XDocument = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                             el0 = new XElement("Belgeler"));
            XNamespace ns = XDocument.Root.Name.Namespace;

            // belgeler
            el0.Add(new XElement("Baslik"));
            el0.Add(new XElement("Detaylar"));

            // baslik
            el0.Element(ns + "Baslik").Add(new XElement("BelgeKodu", XmlConvert.ToString(DocumentID)));
            el0.Element(ns + "Baslik").Add(new XElement("Tesis"));

            using (var dc = new WMSContext())
            {
                foreach (var cmd in Commands)
                    if (cmd.Order_ID.HasValue)
                    {
                        Order order = dc.Orders.Find(cmd.Order_ID.Value);
                        SKU_ID skuid = dc.SKU_IDs.Find(order.SKU_ID);
                        TU tu = dc.TUs.FirstOrDefault(prop => prop.TU_ID == cmd.TU_ID);
                        // Detay
                        el0.Element(ns + "Detaylar").Add(new XElement("Detay"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("BelgeKodu", XmlConvert.ToString(order.ERP_ID.HasValue ? order.ERP_ID.Value : 0)));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("RefBelgeDetayNo"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("UnrunKod", tu.SKU_ID));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("Miktar", XmlConvert.ToString(tu.Qty)));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("Birim", skuid.Unit));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("NetAgirLik"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("AgirlikBirimi"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("KaynakBatchNo"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("HedefBatchNo"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("SeriNo"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("KaynakLokasyon"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("HedefLokasyon", order.Destination));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("KaynakStatus"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("HedefStatu"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("PaletNo", cmd.TU_ID));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("Po"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("PoLine"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("SKT"));
                        (el0.Element(ns + "Detaylar").LastNode as XElement).Add(new XElement("URT"));
                    }
            }
            return XDocument.ToString();
        }
    }
}
