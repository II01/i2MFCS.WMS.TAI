using i2MFCS.WMS.Database.DTO;
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
        [OperationContract(IsOneWay = true)]
        void CommandStatusChanged(int cmdId, int status);
        [OperationContract(IsOneWay = true)]
        void PlaceChanged(string placeID, int TU_ID);
    }

}
