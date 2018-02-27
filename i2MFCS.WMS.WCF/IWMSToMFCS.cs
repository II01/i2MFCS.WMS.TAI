using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.WCF
{
    [ServiceContract]
    public interface IWMSToMFCS
    {
        [OperationContract]
        IEnumerable<Command> GetActiveCommands();
        [OperationContract(IsOneWay = true)]
        void StatusChaned(int cmdId, int status);
        // TODO: Add your service operations here
    }

}
