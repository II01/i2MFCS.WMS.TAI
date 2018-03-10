using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace i2MFCS.WMS.WCF
{
    [ServiceContract]
    public interface IWMSToERP
    {
        [OperationContract(IsOneWay = true)]
        void ErpCommands(string xml);
    }

}
