using i2MFCS.WMS.Database.Tables;
using SimpleLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace i2MFCS.WMS.Core.Xml
{
    public class XmlReadERPCommand : XmlBasicFromERP
    {

        private const string _DeffileNameSchema = @"..\..\..\i2MFCS.WMS.Core\Xml\ERPCommand.xsd";


        public XmlReadERPCommand() : base(_DeffileNameSchema)
        {
        }

        // Test xml->database form->xml

        // read xml->table form
        private int XmlMoveCommand(WMSContext dc, XElement move)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;
                IEnumerable<Order> orders =
                        from order in move.Elements(ns + "Order")
                        from suborder in order.Elements(ns + "SubOrder")
                        from sku in suborder.Elements(ns + "SKU")
                        select new Order
                        {
                            ERP_ID = XmlConvert.ToInt32(move.Element(ns + "ERPID").Value),
                            OrderID = XmlConvert.ToInt32(order.Element(ns + "OrderID").Value),
                            ReleaseTime = XmlConvert.ToDateTime(order.Element(ns + "ReleaseTime").Value, XmlDateTimeSerializationMode.Local),
                            Destination = order.Element(ns + "Location").Value,
                            SubOrderID = XmlConvert.ToInt32(suborder.Element(ns + "SubOrderID").Value),
                            SubOrderName = suborder.Element(ns + "Name").Value,
                            SKU_ID = sku.Element(ns + "SKUID").Value,
                            SKU_Qty = XmlConvert.ToDouble(sku.Element(ns + "Quantity").Value),
                            SKU_Batch = sku.Element(ns + "Batch").Value,
                            Status = 0
                        };

                dc.Orders.AddRange(orders);
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }

        private int XmlDeleteSKUCommand(WMSContext dc, XElement deleteSku)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                foreach (var tuid in deleteSku.Elements(ns + "TU" ))
                {
                    int tuidkey = XmlConvert.ToInt32(tuid.Element(ns + "TUID").Value);
                    IEnumerable<TU> tu = dc.TUs.Where(prop => prop.TU_ID == tuidkey);
                    dc.TUs.RemoveRange(tu);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }

        private int XmlCreateSKUCommand(WMSContext dc, XElement createSku)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                foreach (var tu in createSku.Elements(ns + "TU"))
                    foreach (var sku in tu.Elements(ns + "SKU"))
                        dc.TUs.Add(new TU
                        {
                            TU_ID = XmlConvert.ToInt32(tu.Element(ns + "TUID").Value),
                            SKU_ID = sku.Element(ns + "SKUID").Value,
                            Qty = XmlConvert.ToDouble(sku.Element(ns + "Quantity").Value),
                            Batch = sku.Element(ns + "Batch").Value,
                            ProdDate = XmlConvert.ToDateTime(sku.Element(ns + "ProdDate").Value, XmlDateTimeSerializationMode.Local),
                            ExpDate = XmlConvert.ToDateTime(sku.Element(ns + "ExpDate").Value, XmlDateTimeSerializationMode.Local)
                        });
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }

        private int XmlDeleteTUCommand(WMSContext dc, XElement deleteTU)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                foreach (var tuid in deleteTU.Elements(ns + "TU"))
                {
                    int tuidkey = XmlConvert.ToInt32(tuid.Element(ns + "TUID").Value);
                    IEnumerable<TU> tu = dc.TUs.Where(prop => prop.TU_ID == tuidkey);
                    Place place = dc.Places.Where(prop => prop.TU_ID == tuidkey).First();
                    if (place.PlaceID == tuid.Element(ns + "Location").Value)
                    {
                        dc.Places.Remove(place);
                        dc.TUs.RemoveRange(tu);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw; 
            }
        }

        private int XmlCreateTUCommand(WMSContext dc, XElement createTU)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                foreach (var tu in createTU.Elements(ns + "TU"))
                {
                    int key = XmlConvert.ToInt32(tu.Element(ns + "TUID").Value);
                    TU_ID tuid = dc.TU_IDs.Find(key);
                    if (tuid == null)
                    {
                        tuid = new TU_ID { ID = key };
                        dc.TU_IDs.Add(tuid);
                    }
                    tuid.Blocked = XmlConvert.ToInt32(tu.Element(ns + "Blocked").Value);
                    tuid.DimensionClass = 0;
                    Place p = dc.Places.FirstOrDefault(prop => prop.TU_ID == key);
                    if (p == null)
                    {
                        p = new Place { TU_ID = key };
                        dc.Places.Add(p);
                    }
                    p.PlaceID = tu.Element(ns + "Location").Value;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }

        private int XmlSKUIDUpdateCommand(WMSContext dc, XElement update)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                foreach (var sk in update.Elements(ns + "SKUID"))
                {
                    string key = sk.Element(ns + "ID").Value;
                    SKU_ID skuid = dc.SKU_IDs.Find(key);
                    if (skuid == null)
                    {
                        skuid = new SKU_ID { ID = key };
                        dc.SKU_IDs.Add(skuid);
                    }
                    skuid.Description = sk.Element(ns + "Description").Value;
                    skuid.DefaultQty = XmlConvert.ToDouble(sk.Element(ns + "Quantity").Value);
                    skuid.Unit = sk.Element(ns + "Unit").Value;
                    skuid.Weight = XmlConvert.ToDouble(sk.Element(ns + "Weight").Value);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }

        private int XmlToChangeCommand(WMSContext dc, XElement change)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                foreach (var tu in change.Elements(ns + "TU"))
                {
                    int tuidkey = XmlConvert.ToInt32(tu.Element(ns + "TUID").Value);
                    var p = dc.Places.FirstOrDefault(prop => prop.TU_ID == tuidkey);
                    if (p != null)
                        dc.Places.Remove(p);
                    dc.Places.Add(new Place
                    {
                        PlaceID = tu.Element(ns + "Location").Value,
                        TU_ID = tuidkey,
                    });
                    var tuid = dc.TU_IDs.Find(tuidkey);
                    tuid.Blocked = XmlConvert.ToInt32(tu.Element(ns + "Blocked").Value);
                }
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }

        private int XmlCancelCommand(WMSContext dc, XElement cancel)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                int cancelKey = XmlConvert.ToInt32(cancel.Element(ns + "CommandID").Value);
                var cmd = dc.CommandERP.Find(cancelKey); 
                if (cmd != null && cmd.Status < 3)
                    cmd.Status = 3;
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }

        // todo make reply
        private int XmlStatusCommand(WMSContext dc, XElement status)
        {
            try
            {
                XNamespace ns = XDocument.Root.Name.Namespace;

                int statusKey = XmlConvert.ToInt32(status.Element(ns + "CommandID").Value);
                var cmd = dc.CommandERP.Find(statusKey);
                return 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }


        private void XmlProces()
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    XNamespace ns = XDocument.Root.Name.Namespace;
                    foreach (var cmd in XDocument.Root.Elements())
                    {
                        dc.CommandERP.Add(new CommandERP
                        {
                            ID = XmlConvert.ToInt32(cmd.Element(ns + "ERPID").Value),
                            Command = cmd.ToString(),
                            Status = 0
                        });

                        int status = 0;
                        switch (cmd.Name.LocalName)
                        {
                            case "SKUIDUpdate":
                                status = XmlSKUIDUpdateCommand(dc, cmd);
                                break;
                            case "TUCreate":
                                status = XmlCreateTUCommand(dc, cmd);
                                break;
                            case "TUDelete":
                                status = XmlDeleteTUCommand(dc, cmd);
                                break;
                            case "TUChange":
                                status = XmlToChangeCommand(dc, cmd);
                                break;
                            case "TUCreateSKU":
                                status = XmlCreateSKUCommand(dc, cmd);
                                break;
                            case "TUDeleteSKU":
                                status = XmlDeleteSKUCommand(dc, cmd);
                                break;
                            case "Move":
                                status = XmlMoveCommand(dc, cmd);
                                break;
                            case "Cancel":
                                status = XmlCancelCommand(dc, cmd);
                                break;
                            case "Status":
                                status = XmlStatusCommand(dc, cmd);
                                break;
                        }
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }


        public override void ProcessXml(string xml)
        {
            try
            {
                LoadXml(xml);
                XmlProces();                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Log.AddException(ex, nameof(XmlReadERPCommand));
                throw;
            }
        }


        #region Test xml 
        public void FullTest(string xml)
        {
            LoadXml(xml);
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
                             SuborderID = suborder.Element(ns + "SubOrderID").Value,
                             SuborderName = suborder.Element(ns + "Name").Value,
                             OrderID = order.Element(ns + "OrderID").Value,
                             OrderLocation = order.Element(ns + "Location").Value,
                             OrderReleaseTime = order.Element(ns + "ReleaseTime").Value,
                             ERPID = move.Element(ns + "ERPID").Value
                         }).ToList();

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
                                          orderG.First().OrderLocation,
                                          orderG.First().OrderReleaseTime,
                                          SubOrders = from order in orderG
                                                      group order by order.SuborderID into suborderG
                                                      select new
                                                      {
                                                          SuborderID = suborderG.Key,
                                                          suborderG.First().SuborderName,
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
                foreach (var order in move.Orders)
                {
                    el1.Add(new XElement("Order"));
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
        }

        #endregion

    }

}
