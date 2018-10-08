using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace i2MFCS.WMS.Core.Xml
{
    public class XmlMaterialMoveStatus : XmlBasicToERP
    {
        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\ERPCommandReply.xsd";
        public int DocumentID { get; set; }
        public CommandERP Command { get; set; }

        public XmlMaterialMoveStatus() : base(_DeffileNameSchema)
        { }

        public override string Reference()
        {
            return $"{nameof(XmlMaterialMoveStatus)}(ERP_ID = {Command.ERP_ID}, Status = {Command.Status.ToString()})";
        }

        public override string BuildXml()
        {
            XElement el0 = null;
            XDocument xmlOut = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                             el0 = new XElement("MaterialMoveStatus"));

            el0.Add(new XElement("MFCSID", DocumentID));
            el0.Add(new XElement("Command"));
            XElement el1 = el0.LastNode as XElement;
            el1.Add(new XElement("ERPID", Command.ERP_ID));
            using (var dc = new WMSContext())
            {
                el1.Add(new XElement("OrderID", dc.Orders.FirstOrDefault(p => p.ERP_ID == Command.ID)?.OrderID??0));
            }
            string state;
            switch (Command.Status)
            {
                case CommandERP.CommandERPStatus.NotActive:
                    state = "WAITING";
                    break;
                case CommandERP.CommandERPStatus.Active:
                    state = "EXECUTING";
                    break;
                case CommandERP.CommandERPStatus.Canceled:
                    state = "CANCELED";
                    break;
                case CommandERP.CommandERPStatus.Finished:
                    state = "FINISHED";
                    break;
                default:
                    state = "ERROR";
                    break;
            }
            el1.Add(new XElement("State", state));
            el1.Add(new XElement("Details", ""));
            return xmlOut.ToString();
        }
    }
}
