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
    public class XmlMaterialRetrieved : XmlBasicToERP
    {
        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\WMSWritePickToDocument.xsd";

        public int DocumentID { get; set; }
        public IEnumerable<Command> Commands { get; set; }

        public XmlMaterialRetrieved() : base(_DeffileNameSchema)
        {
        }

        public override string Reference()
        {
            return $"{nameof(XmlMaterialRetrieved)}({String.Join(",", Commands.Select(prop => prop.Box_ID))})";
        }

        public override string BuildXml()
        {
            XElement el0 = null;

//            LoadSchema();
            XDocument XDocument = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                                el0 = new XElement("MaterialRetrieved"));
            el0.Add(new XElement("MFCSID", DocumentID));
            el0.Add(new XElement("MaterialID", Commands.First().Box_ID));
            el0.Add(new XElement("TUID", Commands.First().TU_ID));

            return XDocument.ToString();
        }
    }
}
