using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

namespace i2MFCS.WMS.Core.Xml
{
    public class XmlReadERPCommand
    {

        private string _fileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\ERPCommand.xsd";

        public XDocument XDocument { get; set; }
        private XmlSchemaSet _schema;

        public XmlReadERPCommand()
        {
        }


        public void ReadXml(string xml)
        {
            try
            {
                Debug.WriteLine("Attempting to validate");
                if (_schema == null)
                {
                    _schema = new XmlSchemaSet();
                    _schema.Add(null, _fileNameSchema);
                }

                XDocument = XDocument.Parse(xml);
                bool errors = false;
                XDocument.Validate(_schema, (o, e) =>
                {
                    Debug.WriteLine("{0}", e.Message);
                    errors = true;
                    throw new Exception(e.Message);
                });

                // take all move commands
                XNamespace ns = XDocument.Root.Name.Namespace;
                var fromXml = 
                            (from move in XDocument.Root.Elements(ns + "Move")
                            from order in move.Elements(ns + "Order")
                            from suborder in order.Elements(ns + "SubOrder")
                            from sku in suborder.Elements(ns + "SKU")
                            select new
                            {
                                SKUID = sku.Element(ns + "SKUID").Value,
                                Batch = sku.Element(ns + "Batch").Value,
                                Quantity = sku.Element(ns + "Quantity").Value,
                                ProdDate = sku.Element(ns + "ProdDate").Value,
                                ExpDate = sku.Element(ns + "ExpDate").Value,
                                SubOrderID = suborder.Element( ns + "SubOrderID").Value,
                                SubOrderName = suborder.Element(ns + "Name").Value,
                                OrderID = order.Element(ns + "OrderID").Value,
                                OrderLocation = order.Element(ns + "Location").Value,
                                OrderReleaseTime = order.Element(ns + "ReleaseTime").Value,
                                ERPID = move.Element(ns + "ERPID").Value
                            }).ToList();


                #region test linq to xml
                // prepare move commands back to xml
                // more for testing purposes
                var toXml = (                             
                             from move in fromXml
                             group move by move.ERPID into moveG
                             select new
                             {
                                 ERPID = moveG.Key,
                                 Orders = from order in moveG
                                          group order by order.OrderID into orderG
                                          select new
                                          {
                                              OrderID = orderG.Key,
                                              OrderLocation = orderG.First().OrderLocation,
                                              OrderReleaseTime = orderG.First().OrderReleaseTime,
                                              SubOrders = from order in orderG
                                                          group order by order.SubOrderID into suborderG
                                                          select new
                                                          {
                                                              SuborderID = suborderG.Key,
                                                              SuborderName = suborderG.First().SubOrderName,
                                                              SKUs = suborderG
                                                          }
                                          }
                             });

                XElement el0 = null;
                XDocument xmlOut = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"),
                                                 el0 = new XElement("ERPCommand"));

                foreach (var move in toXml)
                {
                    el0.Add(new XElement("Move"));
                    XElement el1 = el0.LastNode as XElement;
                    el1.Add(new XElement("ERPID", move.ERPID));
                    el1.Add(new XElement("Order"));
                    foreach (var order in move.Orders)
                    {
                        XElement el2 = el1.LastNode as XElement;
                        el2.Add(new XElement("OrderID", order.OrderID));
                        el2.Add(new XElement("ReleaseTime", order.OrderReleaseTime));
                        el2.Add(new XElement("Location", order.OrderLocation));
                        foreach (var subOrder in order.SubOrders)
                        {
                            el2.Add(new XElement("SubOrder"));
                            XElement el3 = el2.LastNode as XElement;
                            el3.Add(new XElement("SubOrderID", subOrder.SuborderID));
                            el3.Add(new XElement("Name", subOrder.SuborderName));
                            foreach (var sku in subOrder.SKUs)
                            {
                                el3.Add(new XElement("SKU"));
                                XElement el4 = el3.LastNode as XElement;
                                el4.Add(new XElement("SKUID", sku.SKUID));
                                el4.Add(new XElement("Quantity", sku.Quantity));
                                el4.Add(new XElement("Batch", sku.Batch));
                                el4.Add(new XElement("ProdDate", sku.ProdDate));
                                el4.Add(new XElement("ExpDate", sku.ExpDate));
                            }
                        }
                    }
                }

                string xmlstr = xmlOut.ToString();
                File.WriteAllText("test.xml", xmlstr);

                #endregion

                Debug.WriteLine("custOrdDoc {0}", errors ? "did not validate" : "validated");
            }
            catch( Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }

}
