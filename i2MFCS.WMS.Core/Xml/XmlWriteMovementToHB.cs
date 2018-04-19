using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace i2MFCS.WMS.Core.Xml
{
    public class XmlWriteMovementToSB : XmlBasicToERP
    {
        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\WMSWriteMovementToSBWithBarcode.xsd";

        public int DocumentID { get; set; }
        public string DocumentType { get; set; }
        public IEnumerable<int> TU_IDs { get; set; }


        public XmlWriteMovementToSB() : base(_DeffileNameSchema)
        {
        }

        public override string Reference()
        {
            return $"{nameof(XmlWriteMovementToSB)}({string.Join(", ",TU_IDs)})";
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
            XElement el1 = (el0.LastNode as XElement);

            // baslik
            el1.Add(new XElement("BelgeKodu", XmlConvert.ToString(DocumentID)));
            el1.Add(new XElement("BelgeTipi", DocumentType));
            el1.Add(new XElement("Tesis", "Aksaray"));
            el1.Add(new XElement("Tarih", XmlConvert.ToString(DateTime.Now, XmlDateTimeSerializationMode.Local)));
/*            el1.Add(new XElement("MusterKodu"));
            el1.Add(new XElement("ReferansNo"));
            el1.Add(new XElement("Aciklama"));
            el1.Add(new XElement("ParcaliIslem")); */
            el0.Add(new XElement("Detaylar"));
            el1 = (el0.LastNode as XElement);

            var idx = 0;
            foreach (var tuid in TU_IDs)
            {
                idx++;
                // Detay
                el1.Add(new XElement("Detay"));
                (el1.LastNode as XElement).Add(new XElement("BelgeKodu", XmlConvert.ToString(DocumentID)));
                (el1.LastNode as XElement).Add(new XElement("DetayNo", XmlConvert.ToString(idx)));
                (el1.LastNode as XElement).Add(new XElement("Barkod", XmlConvert.ToString(tuid)));
            }

/*            el0.Add(new XElement("BaslikEkSahalar"));
            el1 = (el0.LastNode as XElement);
            // BaslikEkSahalar
            el1.Add(new XElement("BaslikEkSaha"));
            (el1.LastNode as XElement).Add(new XElement("BelgeKodu"));
            (el1.LastNode as XElement).Add(new XElement("SahaKodu"));
            (el1.LastNode as XElement).Add(new XElement("SahaDegeri"));

            el0.Add(new XElement("DetayEkSahalar"));
            el1 = (el0.LastNode as XElement);

            // DetayEkSahalar
            el1.Add(new XElement("DetayEkSaha"));
            (el1.LastNode as XElement).Add(new XElement("BelgeKodu"));
            (el1.LastNode as XElement).Add(new XElement("DetayNo"));
            (el1.LastNode as XElement).Add(new XElement("SahaKodu"));
            (el1.LastNode as XElement).Add(new XElement("SahaDegeri"));
*/
            return XDocument.ToString();
        }

    }
}
