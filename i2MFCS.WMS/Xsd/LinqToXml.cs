using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

namespace i2MFCS.WMS.Console
{
    public class LinqToXml
    {
        public string FileNameSchema { get; set; }
        public string FileNameXml { get; set; }

        public XDocument XDocument { get; set; }
        private XmlSchemaSet _schema;

        public LinqToXml()
        {
        }

        public void ReadXml()
        {
            Debug.WriteLine("Attempting to validate");
            _schema = new XmlSchemaSet();
            _schema.Add(null, $@"..\..\Xsd\{FileNameSchema}");

            XDocument = XDocument.Load($@"..\..\Xsd\{FileNameXml}");
            bool errors = false;
            XDocument.Validate(_schema, (o, e) =>
            {
                Debug.WriteLine("{0}", e.Message);
                errors = true;
            });

            XNamespace ns = XDocument.Root.Name.Namespace;
            var linq = (from p in XDocument.Root.Elements().Elements(ns+"SKUIDUpdate") 
                        where p.Name == ns+"SKUIDUpdate"
                        select p.Name.LocalName).ToList();
        
            Debug.WriteLine("custOrdDoc {0}", errors ? "did not validate" : "validated");
        }
    }

}
