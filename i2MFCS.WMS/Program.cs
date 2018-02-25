using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using i2MFCS.WMS.Database.Interface;
using i2MFCS.WMS.Database.Tables;

namespace i2MFCS.WMS
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                DbInterface dc = new DbInterface();
                dc.CreateDatabase();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
