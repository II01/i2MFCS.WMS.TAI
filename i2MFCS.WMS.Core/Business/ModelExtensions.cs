using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Core.Business
{
    public static class ModelExtensions
    {
        public static IEnumerable<Command> ToCommands(this IEnumerable<Place> list, int orderID, string target)
        {
            foreach (Place p in list)
                yield return new Command { TU_ID = p.TU_ID, Source = p.PlaceID, Target = target, Order_ID = orderID, Status = 0 };
        }
    }
}
