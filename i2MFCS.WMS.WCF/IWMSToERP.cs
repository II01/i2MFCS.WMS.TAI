using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace i2MFCS.WMS.WCF
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IWMSToERP
    {
        [OperationContract(IsOneWay = true)]
        void ErpCommands(string xml);

        // TODO: Add your service operations here
    }

}
