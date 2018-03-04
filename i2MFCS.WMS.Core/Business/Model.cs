using i2MFCS.WMS.Core.Business;
using i2MFCS.WMS.Database.DTO;
using i2MFCS.WMS.Database.Tables;
using SimpleLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Interface
{

    public class Model
    {
        private static Random Random = new Random();
        private static object _lockOperation = new Random();
        private static object _lockSingleton = new object();
        private static Model _singleton = null;

        private Timer _timer;


        public Model()
        {
            _timer = new Timer(ActionOnTimer,null,1000,2000);
        }

        public void ActionOnTimer(object state)
        {
            lock (this)
            {
                CreateInputCommand();
                CreateOutputCommands();
            }
        }


        public void CreateDatabase()
        {
            try
            {
                using (WMSContext dc = new WMSContext())
                {
                    dc.Database.Delete();
                    dc.Database.Create();
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public static Model Singleton()
        {
            if (_singleton == null)
                lock (_lockSingleton)
                    if (_singleton == null)
                        _singleton = new Model();
            return _singleton;
        }


        // TODO could also check if material is really there
        public bool CheckIfOrderFinished()
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    int erpID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentERPID").Value);
                    int orderID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentOrderID").Value);
                    int subOrderID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentSubOrderID").Value);

                    bool finished =  !(from order in dc.Orders.Where(order => order.ERP_ID == erpID && order.OrderID == orderID && order.SubOrderID == subOrderID)
                                join cmd in dc.Commands on order.SubOrderID equals cmd.Order_ID
                                where cmd.Status < 4
                                select cmd).Any();

                    return finished;
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        // strict FIFO 
        public void CreateOutputCommands()
        {
            try
            {
                int output = 0;
                List<Command> cmdList = new List<Command>();
                using (var dc = new WMSContext())
                {
                    List<string> target = null;
                    int erpID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentERPID").Value);
                    int orderID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentOrderID").Value);
                    int subOrderID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentSubOrderID").Value);


                    foreach (Order o in dc.Orders.Where((o) => o.Status == 0 && o.ERP_ID == erpID && o.SubOrderID == subOrderID && o.OrderID == orderID && o.ReleaseTime < DateTime.Now))
                    {
                        target = dc.PlaceIds
                                .Where(prop => prop.ID.StartsWith(o.Destination.Substring(0,7)))
                                .Select(prop => prop.ID)
                                .ToList();
                        SKU_ID skuid = dc.SKU_IDs.Find(o.SKU_ID);
                        int count = Convert.ToInt32(o.SKU_Qty / skuid.DefaultQty);
                        if (count > 0)
                        {
                            var newCmd = FIFO_FindSKUWithQty(dc, count, skuid.ID, skuid.DefaultQty, o.SKU_Batch, cmdList.Select(prop => prop.Source));
                            if (newCmd.Count() != count)
                                throw new Exception($"Warehouse does not have enough : {o.SKU_ID} per {skuid.DefaultQty} x {count} {o.SKU_Batch}");
                            cmdList.AddRange(newCmd.ToCommands(o.ID, ""));
                        }
                        if (o.SKU_Qty - count * skuid.DefaultQty > 0)
                        {
                            var newCmd = FIFO_FindSKUWithQty(dc, 1, skuid.ID, o.SKU_Qty - count * skuid.DefaultQty, o.SKU_Batch, cmdList.Select(prop => prop.Source));
                            if (newCmd.Count() != count)
                                throw new Exception($"Warehouse does not have enough : {o.SKU_ID} per {o.SKU_Qty - count * skuid.DefaultQty} x {1} {o.SKU_Batch}");
                            cmdList.Add(newCmd.ToCommands(o.ID, "").First());
                        }
                        o.Status = 1;
                    }

                    if (cmdList.Count > 0)
                    {
                        var cmdSorted = from cmd in cmdList
                                        orderby cmd.Source.EndsWith("1") descending, cmd.Source
                                        select cmd;

                        foreach (Command cmd in cmdSorted)
                        {
                            // Make internal warehouse transfer if neceseary
                            if (cmd.Source.EndsWith("2") && dc.Places.FirstOrDefault(prop => prop.PlaceID == cmd.Source.Substring(0, 10) + ":1") != null &&
                                cmdSorted.FirstOrDefault(prop => prop.Source == cmd.Source.Substring(0, 10) + ":1") == null)
                            {
                                string brother = FindBrotherInside(dc, cmd.TU_ID, cmdSorted.Select(prop => prop.Source));
                                if (brother == null)
                                {
                                    PlaceID nearest = dc.PlaceIds.Find(cmd.Source);
                                    int tu_id = dc.Places.FirstOrDefault(prop => prop.PlaceID == nearest.ID).TU_ID;
                                    brother = FindFreeNearestWindow(dc, dc.TU_IDs.Find(cmd.TU_ID).DimensionClass, cmdSorted.Select(prop => prop.Source), nearest);
                                    if (brother == null)
                                        throw new Exception("Warehouse is full");
                                    Command transferCmd = new Command
                                    {
                                        Order_ID = cmd.Order_ID,
                                        TU_ID = tu_id,
                                        Source = cmd.Source.Substring(0, 10) + ":1",
                                        Target = brother,
                                        Status = 0
                                    };
                                    dc.Commands.Add(transferCmd);
                                    Log.AddLog(Log.Severity.EVENT, nameof(Model), $"Transfer command created ({transferCmd.ToString()})", "");
                                    Debug.WriteLine($"Transfer command created ({transferCmd.ToString()})");
                                }
                            }
                            // Assign targets to
                            cmd.Target = target[output];
                            output = (output + 1) % target.Count;
                            dc.Commands.Add(cmd);
                            Log.AddLog(Log.Severity.EVENT, nameof(Model), $"Order command create ({cmd.ToString()})", "");
                            Debug.WriteLine($"Order commad created ({cmd.ToString()})");
                        }
                        // EF6 uses transaction -- it should rollback id not sucesfull
                        // TODO uncomment SaveChanged
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void CreateInputCommand()
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    string source = dc.Parameters.Find("InputCommand.Place").Value;
                    Command cmd = null;
                    Place place = dc.Places.FirstOrDefault(prop => prop.PlaceID == source);
                    if (place != null && !place.FK_PlaceID.FK_Source_Commands.Any(prop => prop.Status < 3) 
                        && place.FK_TU_ID.FK_TU.Any())
                    {
                        string brother = FindBrotherInside(dc, place.TU_ID, new List<string>());
                        if (brother != null)
                        {
                            cmd = new Command
                            {
                                Order_ID = null,
                                TU_ID = place.TU_ID,
                                Source = source,
                                Target = brother,
                                Status = 0
                            };
                        }
                        else
                        {
                            PlaceID tar = FindFreeRandomWindow(dc, place.FK_TU_ID.DimensionClass, new List<string>());
                            if (tar == null)
                                throw new Exception("Warehouse is full");
                            cmd = new Command
                            {
                                Order_ID = null,
                                TU_ID = place.TU_ID,
                                Source = source,
                                Target = tar.ID,
                                Status = 0
                            };
                        }
                        if (cmd != null)
                        {
                            dc.Commands.Add(cmd);
                            dc.SaveChanges();
                            Debug.WriteLine($"Input command for {source} crated : {cmd.ToString()}");
                            Log.AddLog(Log.Severity.EVENT, nameof(Model), $"Input command for {source} crated : {cmd.ToString()}", "");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        private string FindBrotherInside(WMSContext dc, int barcode, IEnumerable<string> forbidden)
        {
            try
            {
                TU lookFor = dc.TUs.First(prop => prop.TU_ID == barcode);
                List<string> inputWh = new List<string> { "W:11", "W:12", "W:21", "W:22" };

                return  (
                        from list in
                        (from place in dc.Places
                         join neighbour in dc.PlaceIds on place.PlaceID.Substring(0, 10) + ":1" equals neighbour.ID
                         where inputWh.Any(prop => place.PlaceID.StartsWith(prop)) && place.PlaceID.EndsWith(":2")
                                 && !place.FK_PlaceID.FK_Source_Commands.Any(prop => prop.Status < 3) 
                                 && !neighbour.FK_Places.Any() 
                                 && !neighbour.FK_Target_Commands.Any(prop => prop.Status < 3) 
                                 && (!forbidden.Any(prop => place.PlaceID.StartsWith(prop)))
                         select new
                         {
                             PlaceID = place.PlaceID, 
                             TU_ID = place.TU_ID
                         }
                        ).Union
                        ( 
                        from cmd in dc.Commands
                        join neighbour in dc.PlaceIds on cmd.Target.Substring(0,10) + ":1" equals neighbour.ID
                        join pID in dc.PlaceIds on cmd.Target equals pID.ID
                        where cmd.Status < 3 && inputWh.Any(prop => cmd.Target.StartsWith(prop)) && cmd.Target.EndsWith(":2") 
                                && !neighbour.FK_Places.Any() 
                                && !neighbour.FK_Target_Commands.Any(prop => prop.Status < 3) 
                                && (!forbidden.Any(prop => cmd.Target.StartsWith(prop)))
                        select new
                        {
                            PlaceID = cmd.Target,
                            TU_ID = cmd.TU_ID
                        }
                        )
                        join tu in dc.TUs on list.TU_ID equals tu.TU_ID
                         // TODO - introduce tolerance time
                         where tu.Batch == lookFor.Batch && tu.SKU_ID == lookFor.SKU_ID && tu.Qty == lookFor.Qty
                         orderby DbFunctions.DiffHours(lookFor.ProdDate, tu.ProdDate)
                         select list.PlaceID).FirstOrDefault();

            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        // TODO : solution with group is slower then with neigbour join 
        private PlaceID FindFreeRandomWindow(WMSContext dc, int dimensionclass, IEnumerable<string> forbidden)
        {
            try
            {
                List<string> inputWh = new List<string> { "W:11", "W:12", "W:21", "W:22" };

                var linq1 = 
                        (from placeID in dc.PlaceIds
                         where
                             !placeID.FK_Places.Any()
                             && placeID.DimensionClass == dimensionclass
                             && inputWh.Any(p1 => placeID.ID.StartsWith(p1))
                             && !placeID.FK_Target_Commands.Any(prop => prop.Status < 3)
                             && !forbidden.Any((prop) => placeID.ID.StartsWith(prop))
                         group placeID by placeID.ID.Substring(0, 10) into g
                         where g.Count() == 2
                         orderby g.Key
                         select g);

                int count = linq1.Count();
                if (count != 0)
                    return linq1.Skip(Random.Next(count - 1)).First().Last();

                var linq2 =
                        (from placeID in dc.PlaceIds
                         where
                             !placeID.FK_Places.Any()
                             && placeID.DimensionClass == dimensionclass
                             && inputWh.Any(p1 => placeID.ID.StartsWith(p1))
                             && !placeID.FK_Target_Commands.Any(prop => prop.Status < 3)
                             && !forbidden.Any((prop) => placeID.ID.StartsWith(prop))
                         group placeID by placeID.ID.Substring(0, 10) into g
                         orderby g.Key
                         select g);
                count = linq2.Count();
                if (count != 0)
                    return linq1.Skip(Random.Next(count - 1)).First().First();

                return null;
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private string FindFreeNearestWindow(WMSContext dc, int dimensionclass, IEnumerable<string> forbidden, PlaceID place)
        {
            try
            {
                List<string> inputWh = new List<string> { "W:11", "W:12", "W:21", "W:22" };

                var linq1 =
                        (from placeID in dc.PlaceIds
                         join neighbour in dc.PlaceIds on place.ID.Substring(0,10)+":1" equals neighbour.ID
                         where
                             !placeID.FK_Places.Any()
                             && !neighbour.FK_Places.Any()
                             && placeID.ID.EndsWith(":2")
                             && placeID.DimensionClass == dimensionclass
                             && inputWh.Any(p1 => placeID.ID.StartsWith(p1))
                             && !placeID.FK_Target_Commands.Any(prop => prop.Status < 3)
                             && !forbidden.Any((prop) => placeID.ID.StartsWith(prop))
                             && placeID.ID.StartsWith(place.ID.Substring(0,3))
                             && placeID.ID.Substring(0,10) != place.ID.Substring(0,10)
                         orderby (placeID.PositionHoist - place.PositionHoist) * (placeID.PositionHoist - place.PositionHoist) +
                                  (placeID.PositionTravel - place.PositionTravel) * (placeID.PositionTravel - place.PositionTravel) ascending
                         select placeID).FirstOrDefault();

                if (linq1 != null)
                    return linq1.ID;

                var linq2 =
                        (from placeID in dc.PlaceIds
                         where
                             !placeID.FK_Places.Any()
                             && placeID.ID.EndsWith(":2")
                             && placeID.DimensionClass == dimensionclass
                             && inputWh.Any(p1 => placeID.ID.StartsWith(p1))
                             && !placeID.FK_Target_Commands.Any(prop => prop.Status < 3)
                             && !forbidden.Any((prop) => placeID.ID.StartsWith(prop))
                             && placeID.ID.StartsWith(place.ID.Substring(0, 3))
                             && placeID.ID.Substring(0, 10) != place.ID.Substring(0, 10)
                         orderby (placeID.PositionHoist - place.PositionHoist) * (placeID.PositionHoist - place.PositionHoist) +
                                  (placeID.PositionTravel - place.PositionTravel) * (placeID.PositionTravel - place.PositionTravel) ascending
                         select placeID).FirstOrDefault();

                if (linq2 != null)
                    return linq2.ID;

                return null;
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        private IEnumerable<Place> FIFO_FindSKUWithQty(WMSContext dc, int count, string lookForSKUID, double lookForQty, string batch, IEnumerable<string> forbidden)
        {
            try
            {
                var linq = (
                            from p in dc.Places
                            join tu in dc.TUs on p.TU_ID equals tu.TU_ID
                            join neigh in dc.PlaceIds on p.PlaceID.Substring(0,10)+":1" equals neigh.ID
                            where tu.SKU_ID == lookForSKUID && tu.Qty == lookForQty && tu.Batch == batch         
                                  && !p.FK_PlaceID.FK_Source_Commands.Any(prop=>prop.Status<3) 
                                  && !neigh.FK_Target_Commands.Any(prop=>prop.Status<3)
                                  && !forbidden.Any(prop => p.PlaceID == prop)
                            // TODO make tolerance to access outer SKUS's if possible
                            orderby tu.ProdDate ascending          // strict FIFO 
                            select p)
                           .Take(count).ToList();
                return linq;
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        public IEnumerable<DTOCommand> GetNewCommands()
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    var cmd = dc.Commands.Where(prop => prop.Status == 1)
                            .Select(prop => prop).ToList();
                    cmd.ForEach(prop => prop.Status = 1);
                    dc.SaveChanges();
                    return (from c in cmd
                            select new DTOCommand
                            {
                                ID = c.ID,
                                Order_ID = c.Order_ID,
                                Source = c.Source,
                                Status = c.Status,
                                Target = c.Target,
                                TU_ID = c.TU_ID
                            }).ToList();
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void MFCSUpdatePlace(string placeID, int TU_ID)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    Place p = dc.Places
                                .Where(prop => prop.TU_ID == TU_ID)
                                .FirstOrDefault();
                    if (p != null)
                        dc.Places.Remove(p);
                    dc.Places.Add(new Place
                    {
                        PlaceID = placeID,
                        TU_ID = TU_ID
                    });
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        public void MFCSUpdateCommand(int commandID, int status)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    dc.Commands.Find(commandID).Status = status;
                    dc.SaveChanges();
                }
                // TODO Check orders finished
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }



    }
}
