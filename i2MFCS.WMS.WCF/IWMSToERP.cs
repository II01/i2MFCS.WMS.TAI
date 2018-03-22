using i2MFCS.WMS.Core.Xml.ERPCommand;
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
        [OperationContract(IsOneWay = false)]
        string ErpCommands(string xml);

        [OperationContract(IsOneWay = false)]
        ERPSubmitStatus ErpCommandsS(ERPCommand erpCommands);

    }

}
