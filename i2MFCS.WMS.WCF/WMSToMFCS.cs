using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using i2MFCS.WMS.Core.Business;
using i2MFCS.WMS.Database.DTO;
using i2MFCS.WMS.Database.Tables;
using SimpleLogs;

namespace i2MFCS.WMS.WCF
{

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.PerSession)]
    public class WMSToMFCS : IWMSToMFCS, IDisposable
    {
        protected Model Model { get; }


        public WMSToMFCS()
        {
        }

        void IWMSToMFCS.CommandStatusChanged(int cmdId, int status)
        {
            try
            {
                if(cmdId >= 0)
                    Model.Singleton().UpdateCommand(cmdId, status);
            }
            catch (Exception ex)
            {
                SimpleLog.AddException(ex, nameof(WMSToMFCS));
                Debug.WriteLine(ex.Message);
                throw new FaultException(ex.Message);
            }
        }

        void IWMSToMFCS.PlaceChanged(string placeID, int TU_ID, int dim, string changeType)
        {
            try
            {
                Model.Singleton().UpdatePlace(placeID, TU_ID, dim, changeType);
            }
            catch (Exception ex)
            {
                SimpleLog.AddException(ex, nameof(WMSToMFCS));
                Debug.WriteLine(ex.Message);
                throw new FaultException(ex.Message);
            }
        }

        void IWMSToMFCS.DestinationEmptied(string place)
        {
            try
            {
                Model.Singleton().ReleaseRamp(place);
            }
            catch (Exception ex)
            {
                SimpleLog.AddException(ex, nameof(WMSToMFCS));
                Debug.WriteLine(ex.Message);
                throw new FaultException(ex.Message);
            }
        }

        bool IWMSToMFCS.OrderForRampActive(string ramp)
        {
            try
            {
                return Model.Singleton().OrderForRampActive(ramp);
            }
            catch (Exception ex)
            {
                SimpleLog.AddException(ex, nameof(WMSToMFCS));
                Debug.WriteLine(ex.Message);
                throw new FaultException(ex.Message);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~WMSToMFCS() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
