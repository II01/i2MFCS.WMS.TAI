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
        [OperationContract]
        void CommandStatusChanged(int cmdId, int status);

        [OperationContract]
        void PlaceChanged(string placeID, int TU_ID, int TU_dimension, string changeType);
    }

}
