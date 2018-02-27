using i2MFCS.WMS.WCF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Service
{
    public partial class Service : ServiceBase
    {
        private ServiceHost _wmsToErp = null;
        private ServiceHost _wmsToMFCS = null;
        private ServiceHost _wmsToUI = null;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

                _wmsToErp = new ServiceHost(typeof(WMSToERP));
                _wmsToMFCS = new ServiceHost(typeof(WMSToMFCS));
                _wmsToUI = new ServiceHost(typeof(WMSToUI));
                _wmsToErp.Open();
                _wmsToMFCS.Open();
                _wmsToUI.Open();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnStop()
        {
            try
            {
                _wmsToUI?.Close();
                _wmsToMFCS?.Close();
                _wmsToErp?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
