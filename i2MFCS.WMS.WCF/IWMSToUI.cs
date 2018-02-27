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
    public interface IWMSToUI
    {
        [OperationContract]
        IEnumerable<Command> GetCommands();
    }
}
