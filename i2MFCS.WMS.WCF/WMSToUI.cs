using i2MFCS.WMS.Core.Business;
using i2MFCS.WMS.Core.DataExchange;
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

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single, InstanceContextMode = InstanceContextMode.PerSession)]
    public class WMSToUI : IWMSToUI, IDisposable
    {
        IEnumerable<DTOCommand> IWMSToUI.GetCommands()
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void IWMSToUI.CancelOrder(DTOOrder order)
        {
            Model.Singleton().CancelOrderCommands(order);
        }

        void IWMSToUI.BlockLocations(string locStartsWith, bool block, int reason)
        {
            Model.Singleton().BlockLocations(locStartsWith, block, reason);
        }

        void IWMSToUI.BlockTU(int TUID, bool block, int reason)
        {
            Model.Singleton().BlockTU(TUID, block, reason);
        }
        void IWMSToUI.CancelCommand(DTOCommand cmd)
        {
            Model.Singleton().CancelCommand(cmd);
        }

        void IWMSToUI.ReleaseRamp(string destinationStartsWith)
        {
            Model.Singleton().ReleaseRamp(destinationStartsWith);
        }

        public void UpdatePlace(List<PlaceDiff> diffs, string user)
        {
            Model.Singleton().SyncDatabase(diffs, user);
        }

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
        // ~WMSToUI() {
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
