using SimpleLogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

namespace i2MFCS.WMS.Core.Xml
{

    public abstract class XmlBasic
    {
        private string _fileNameSchema;

        protected XDocument XDocument { get; set; }
        protected XmlSchemaSet _schema;

        public XmlBasic(string fileName)
        {
            _fileNameSchema = fileName;
        }

        protected void LoadSchema()
        {
            if (_schema == null)
            {
                _schema = new XmlSchemaSet();
                _schema.Add(null, _fileNameSchema);
            }
        }

        protected void LoadXml(string xml)
        {
            LoadSchema();
            XDocument = XDocument.Parse(xml);
            XDocument.Validate(_schema, (o, e) =>
            {
                Debug.WriteLine($"{e.Message}");
                Exception ex = new Exception(e.Message);
                SimpleLog.AddException(ex, nameof(XmlBasic));
                throw ex;
            });
        }

    }

    public abstract class XmlBasicFromERP : XmlBasic
    {
        public XmlBasicFromERP(string fileName) : base(fileName)
        { }
        public abstract void ProcessXml(string xml);
        public abstract string Reference();
    }

    public abstract class XmlBasicToERP : XmlBasic
    {
        public XmlBasicToERP(string fileName) : base(fileName)
        { }
        public abstract string BuildXml();
        public abstract string Reference();
    }
}
