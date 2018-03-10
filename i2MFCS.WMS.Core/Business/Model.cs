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
                using (var dc = new WMSContext())
                {
                    int erpID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentERPID").Value);
                    int orderID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentOrderID").Value);
                    int subOrderID = Convert.ToInt32(dc.Parameters.Find("Order.CurrentSubOrderID").Value);



                    /// Alternative faster solution
                    /// Create DTOOrders from Orders
                    DateTime now = DateTime.Now;
                    List<DTOOrder> dtoOrders =
                        dc.Orders
                        .Where(prop => prop.Status == 0 && prop.ERP_ID == erpID && prop.OrderID == orderID && prop.SubOrderID == subOrderID && prop.ReleaseTime < now)
                        .OrderToDTOOrders()                        
                        .ToList();


                    // create DTO commands
                    List<DTOCommand> cmdList =
                        dtoOrders
                        .DTOOrderToDTOCommand()
                        .ToList();


                    if (cmdList.Count > 0)
                    {
                        var cmdSortedFromOne = cmdList
                                        .OrderByDescending(prop => prop.Source.EndsWith("1"))
                                        .ThenByDescending(prop => prop.Source);


                        List<DTOCommand> transferProblemCmd = cmdList
                                         .Where(prop => prop.Source.EndsWith("2"))
                                         .Where(prop => !cmdList.Any(cmd => cmd.Source == prop.Source.Substring(0, 10) + ":1"))
                                         .Join( dc.Places, 
                                                command => command.Source.Substring(0,10) + ":1", 
                                                neighbour => neighbour.PlaceID,
                                                (command, neighbour) => new {Command = command, Neighbour = neighbour}
                                         )
                                         .Where(prop => !prop.Neighbour.FK_PlaceID.FK_Source_Commands.Any())
                                         .Select(prop=>prop.Command)
                                         .ToList();

                        List<DTOCommand> transferCmd = transferProblemCmd
                                        .TakeNeighbour()
                                        .MoveToBrotherOrFree()
                                        .ToList();

                        dc.SaveChanges();
                        foreach (var cmd in cmdSortedFromOne)
                        {
                            int i = transferProblemCmd.IndexOf(cmd);
                            if (i != -1)
                            {
                                Debug.WriteLine($"Transfer command : {transferCmd[i].ToString()}");
                                Log.AddLog(Log.Severity.EVENT, nameof(Model), $"Transfer command : {transferCmd[i].ToString()}", "");
                                dc.Commands.Add(transferCmd[i].ToCommand());
                            }
                            Debug.WriteLine($"Output command : {cmd.ToString()}");
                            Log.AddLog(Log.Severity.EVENT, nameof(Model), $"Transfer command : {cmd.ToString()}", "");
                            dc.Commands.Add(cmd.ToCommand());
                        }
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
                    List<string> forbidden = new List<string>();
                    Place place = dc.Places.FirstOrDefault(prop => prop.PlaceID == source);
                    if (place != null && !place.FK_PlaceID.FK_Source_Commands.Any(prop => prop.Status < 3) 
                        && place.FK_TU_ID.FK_TU.Any())
                    {
                        var cmd = new DTOCommand
                        {
                            Order_ID = null,
                            TU_ID = place.TU_ID,
                            Source = source,
                            Target = null,
                            Status = 0
                        };
                        cmd.Target = cmd.GetRandomPlace(forbidden);
                        dc.Commands.Add(cmd.ToCommand());
                        dc.SaveChanges();
                        Debug.WriteLine($"Input command for {source} crated : {cmd.ToString()}");
                        Log.AddLog(Log.Severity.EVENT, nameof(Model), $"Input command for {source} crated : {cmd.ToString()}", "");
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
