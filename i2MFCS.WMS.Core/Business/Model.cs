using i2MFCS.WMS.Core.DataExchange;
using i2MFCS.WMS.Database.DTO;
using i2MFCS.WMS.Database.Tables;
using SimpleLogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Core.Business
{

    public class Model
    {
        public enum BoxNotifyType { Entry = 0, Drop, Pick }

        private static Random Random = new Random();
        private static object _lockSingleton = new object();
        private static object _lockTimer = new object();
        private static Model _singleton = null;

        private Timer _timer;
        private SimulateERP _simulateERP;

        private string _erpUser;
        private string _erpPwd;
        private byte _erpCode = 0;
        private Random _rnd = new Random();

        public Model()
        {
            _timer = new Timer(ActionOnTimer, null, 1000, 2000);
            _simulateERP = new SimulateERP();

            _erpUser = ConfigurationManager.AppSettings["erpUser"];
            _erpPwd = ConfigurationManager.AppSettings["erpPwd"];
            byte.TryParse(ConfigurationManager.AppSettings["erpCode"], out _erpCode);
        }

        public void ActionOnTimer(object state)
        {
            lock (_lockTimer)
            {
                try
                {
                    CreateCommands();
                    ExecuteUICommands();
                }
                catch (Exception ex)
                {
                    Log.AddException(ex);
                    SimpleLog.AddException(ex, nameof(Model));
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        // strict FIFO 
        public void CreateCommands()
        {
            try
            {
                using (var dc = new WMSContext())
                using (var ts = dc.Database.BeginTransaction())
                {
                    try
                    {
                        DateTime now = DateTime.Now;
                        List<Order> findOrders = new List<Order>();

                        if (!dc.Orders.Any(p => p.Status == Order.OrderStatus.Active))
                        {
                            var placeIO = dc.Parameters.Find("Place.IOStation").Value;
                            var stationTUID = dc.Places.FirstOrDefault(p => p.PlaceID == placeIO)?.TU_ID;

                            if( stationTUID != null )
                            {
                                findOrders = dc.Orders
                                    .Where(p => p.Status >= Order.OrderStatus.NotActive && p.Status < Order.OrderStatus.Canceled)
                                    .Where(p => p.TU_ID == stationTUID)
                                    .ToList();
                            }
                            else
                            {
                                findOrders = dc.Orders
                                    .Where(p => p.Status >= Order.OrderStatus.NotActive && p.Status < Order.OrderStatus.Canceled)
                                    .GroupBy(
                                        (by) => new { by.Destination },
                                        (key, group) => new
                                        {
                                            CurrentOrder = group.FirstOrDefault(p => p.Status > Order.OrderStatus.NotActive && p.Status < Order.OrderStatus.Canceled),
                                            CurrentSubOrder = group.FirstOrDefault(p => p.Status > Order.OrderStatus.NotActive && p.Status < Order.OrderStatus.OnTargetPart),
                                            NewOrder = group.FirstOrDefault(p => p.Status == Order.OrderStatus.NotActive),
                                            Group = group
                                        }
                                    )
                                    .Where(p => p.NewOrder != null)
                                    .Where(p => p.CurrentOrder == null || (p.CurrentOrder.ERP_ID == p.NewOrder.ERP_ID && p.CurrentOrder.OrderID == p.NewOrder.OrderID))
                                    .Where(p => (p.CurrentSubOrder == null || (p.CurrentSubOrder.ERP_ID == p.CurrentOrder.ERP_ID && p.CurrentSubOrder.OrderID == p.CurrentOrder.OrderID && p.NewOrder.SubOrderID == p.CurrentSubOrder.SubOrderID)))
                                    .SelectMany(p => p.Group
                                                        .Where(pp => pp.ERP_ID == p.NewOrder.ERP_ID && pp.OrderID == p.NewOrder.OrderID && pp.SubOrderID == p.NewOrder.SubOrderID && pp.TU_ID == p.NewOrder.TU_ID)
                                               )
                                    //                                .Where(p => p.ReleaseTime < now)
                                    .ToList();
                            }

                            foreach (var o in findOrders)
                                o.Status = Order.OrderStatus.Active;
                        }

                        /// Create DTOOrders from Orders
                        List<DTOOrder> dtoOrders =
                            findOrders
                            .OrderToDTOOrders()
                            .OrderBy(p => p.ID)
                            .ThenBy(p => p.Operation)
                            .ToList();

                        // create DTO commands
                        List<DTOCommand> cmdList =
                            dtoOrders
                            .DTOOrderToDTOCommand()
                            .OrderBy(p => p.ID)
                            .ThenBy(p => p.Operation)
                            .ToList();


                        if (cmdList.Count > 0)
                        {
                            var commands = cmdList.ToCommand().ToList();
                            dc.Commands.AddRange(commands);
                            // notify ERP about changes
                            Log.AddLog(Log.SeverityEnum.Event, "gencmd", "gencmd");

                            dc.SaveChanges();
                            using (MFCS_Proxy.WMSClient proxy = new MFCS_Proxy.WMSClient())
                            {
                                 proxy.MFCS_Submit(commands.Where(p => p.Operation == Command.CommandOperation.MoveTray && p.Status == Command.CommandStatus.NotActive).ToProxyDTOCommand().ToArray());
                            }
                            ts.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        ts.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLog.AddException(ex, nameof(Model));
                Log.AddException(ex);
                Debug.WriteLine(ex.Message);
            }
        }

        public void ExecuteUICommands()
        {
            try
            {
                // if station occupied and all previous command finished -> put station commands to active
                using (var dc = new WMSContext())
                {
                    string placeIO = dc.Parameters.Find("Place.IOStation").Value;
                    string placeOut = dc.Parameters.Find("Place.OutOfWarehouse").Value;

                    // get commands of the active order
                    var commands = dc.Orders
                                    .Where(p => p.Status == Order.OrderStatus.Active)
                                    .Join(dc.Commands,
                                        o => o.ID,
                                        c => c.Order_ID,
                                        (o, c) => new { Ord = o, Cmd = c })
                                    .Select(p => p.Cmd);
                    var cc = commands.ToList();

                    var place = dc.Places.FirstOrDefault(p => p.PlaceID == placeIO);
                    // if something on the working station set station commands to active
                    if (place != null)
                    {
                        var commandsAction = commands
                                                .Where(p => p.Operation == Command.CommandOperation.StoreTray || p.Operation == Command.CommandOperation.RetrieveTray ||
                                                            p.Operation == Command.CommandOperation.DropBox || p.Operation == Command.CommandOperation.PickBox)
                                                .Where(p => p.TU_ID == place.TU_ID)
                                                .Where(p => p.Status == Command.CommandStatus.NotActive);
                        foreach (var ca in commandsAction)
                            UpdateCommand(ca.ID, (int)Command.CommandStatus.Active);
                    }

                    // Tray commands
                    if (place != null)
                    { 
                        // put storeTray command to finish
                        var commandStoreTray = commands.FirstOrDefault(p => p.Operation == Command.CommandOperation.StoreTray && p.Status == Command.CommandStatus.Active);
                        if (commandStoreTray != null && commandStoreTray.TU_ID == place.TU_ID)
                            UpdateCommand(commandStoreTray.ID, (int)Command.CommandStatus.Finished);
                    }
                    else
                    {
                        // put retrieveTray command to finish
                        var commandRetrieveTray = commands.FirstOrDefault(p => p.Operation == Command.CommandOperation.RetrieveTray && p.Status == Command.CommandStatus.Active);
                        if (commandRetrieveTray != null && dc.Places.Where(p => p.PlaceID == placeOut && p.TU_ID == commandRetrieveTray.TU_ID) != null)
                            UpdateCommand(commandRetrieveTray.ID, (int)Command.CommandStatus.Finished);
                    }

                    // Confirms
                    // put confirmStore command to finish (TODO: to active, if needed)
                    var commandConfirmStore = commands.FirstOrDefault(p => p.Operation == Command.CommandOperation.ConfirmStore && p.Status < Command.CommandStatus.Active);
                    if (commandConfirmStore != null && !commands.Any(p => p.Operation < Command.CommandOperation.ConfirmStore && p.Status <= Command.CommandStatus.Active))
                        UpdateCommand(commandConfirmStore.ID, (int)Command.CommandStatus.Finished);
                    // put confirmFinish command to active
                    var commandConfirmFinish = commands.FirstOrDefault(p => p.Operation == Command.CommandOperation.ConfirmFinish && p.Status < Command.CommandStatus.Active);
                    if (commandConfirmFinish != null && !commands.Any(p => p.Operation < Command.CommandOperation.ConfirmFinish && p.Status <= Command.CommandStatus.Active))
                        UpdateCommand(commandConfirmFinish.ID, (int)Command.CommandStatus.Active);
                }
            }
            catch (Exception ex)
            {
                SimpleLog.AddException(ex, nameof(Model));
                Log.AddException(ex);
                Debug.WriteLine(ex.Message);
            }
        }

        public void StoreTUID(int tuid)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    var la = dc.Orders
                                .Where(p => p.ERP_ID == null)
                                .OrderByDescending(p => p.OrderID)
                                .FirstOrDefault();
                    var lh = dc.HistOrders
                                .Where(p => p.ERP_ID == null)
                                .OrderByDescending(p => p.OrderID)
                                .FirstOrDefault();
                    int lastUsedOrderID = Math.Max(la == null ? 0 : la.OrderID, lh == null ? 0 : lh.OrderID);

                    if (dc.TU_IDs.Find(tuid) == null)
                        dc.TU_IDs.Add(new TU_ID
                        {
                            ID = tuid,
                            DimensionClass = 0,
                            Blocked = 0
                        });
                    dc.Orders.Add(new Order
                    {
                        ERP_ID = null,
                        OrderID = lastUsedOrderID + 1,
                        SubOrderID = 1,
                        SubOrderName = "Store TUID",
                        TU_ID = tuid,
                        Box_ID = "-",
                        SKU_ID = "-",
                        SKU_Batch = "-",
                        Destination = dc.Parameters.Find("Place.IOStation").Value,
                        Operation = Order.OrderOperation.StoreTray,
                        Status = Order.OrderStatus.NotActive,
                        ReleaseTime = DateTime.Now
                    });
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public int SuggestTUID(List<string> boxes)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    int hc = 1;
                    foreach (var b in boxes)
                        hc = Math.Max(hc, dc.Box_IDs.Find(b).FK_SKU_ID.Height);
                    var suitable = dc.Places
                                    .Where(p => p.PlaceID.StartsWith("W:") && p.FK_PlaceID.DimensionClass >= hc)
                                    .Where(p => !p.FK_TU_ID.FK_TU.Any())
                                    .OrderBy(p => p.FK_PlaceID.DimensionClass);

                    var selected = suitable.FirstOrDefault();
                    if (selected != null)
                        return selected.TU_ID;
                    else
                        return 0;
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
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
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
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

        public void MoveOrderToHist(int? erpid, int orderid)
        {
            try
            {
                using (var dc = new WMSContext())
                {

                    bool boolOrdersFinished = !dc.Orders.Any(prop => prop.ERP_ID == erpid && prop.OrderID == orderid &&
                                                             prop.Status < Order.OrderStatus.Canceled);
                    if (boolOrdersFinished)
                    {
                        var orders = dc.Orders.Where(prop => prop.ERP_ID == erpid && prop.OrderID == orderid);
                        foreach (var o in orders)
                        {
                            dc.HistOrders.Add(new HistOrder
                            {
                                ID = o.ID,
                                ERP_ID = o.ERP_ID,
                                OrderID = o.OrderID,
                                SubOrderERPID = o.SubOrderERPID,
                                SubOrderID = o.SubOrderID,
                                SubOrderName = o.SubOrderName,
                                Operation = (HistOrder.HistOrderOperation)o.Operation,
                                TU_ID = o.TU_ID,
                                SKU_ID = o.SKU_ID,
                                SKU_Batch = o.SKU_Batch,
                                SKU_Qty = o.SKU_Qty,
                                Box_ID = o.Box_ID,
                                Destination = o.Destination,
                                ReleaseTime = o.ReleaseTime,
                                Status = (HistOrder.HistOrderStatus)o.Status,
                            });

                            var cmds = dc.Commands.Where(p => p.Order_ID == o.ID);
                            foreach (var c in cmds)
                                dc.HistCommands.Add(new HistCommand
                                {
                                    ID = c.ID,
                                    Order_ID = c.Order_ID,
                                    Operation = (HistCommand.HistCommandOperation)c.Operation,
                                    TU_ID = c.TU_ID,
                                    Box_ID = c.Box_ID,
                                    Source = c.Source,
                                    Target = c.Target,
                                    Status = (HistCommand.HistCommandStatus)c.Status,
                                    Time = c.Time,
                                    LastChange = c.LastChange
                                });

                            dc.Commands.RemoveRange(cmds);
                            dc.Orders.Remove(o);
                        }
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(MoveOrderToHist));
                Debug.WriteLine(ex.Message);
            }
        }

        public void UpdateCommand(int id, int status)
        {
            try
            {
                {
                    using (var dc = new WMSContext())
                    {
                        var cmd = dc.Commands.Find(id);
                        if (cmd != null)
                        {
                            cmd.Status = (Command.CommandStatus)status;
                            cmd.LastChange = DateTime.Now;
                            dc.SaveChanges();
                            Log.AddLog(Log.SeverityEnum.Event, nameof(UpdateCommand), $"{id}, {status}");
                            // move canceled/finished command which are not part of an order to HistCommands
                            if (cmd.Order_ID == null && cmd.Status >= Command.CommandStatus.Canceled)
                            {
                                dc.HistCommands.Add(new HistCommand
                                {
                                    ID = cmd.ID,
                                    Order_ID = cmd.Order_ID,
                                    TU_ID = cmd.TU_ID,
                                    Box_ID = cmd.Box_ID,
                                    Source = cmd.Source,
                                    Target = cmd.Target,
                                    Operation = (HistCommand.HistCommandOperation)cmd.Operation,
                                    Status = (HistCommand.HistCommandStatus)cmd.Status,
                                    Time = cmd.Time,
                                    LastChange = cmd.LastChange
                                });
                                dc.Commands.Remove(cmd);
                                dc.SaveChanges();

                            }
                            UpdateOrderAndCommandERP(cmd);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public bool UpdateOrderAndCommandERP(Command command)
        {
            try
            {
                int? erpid = null;
                int orderid = 0;

                using (var dc = new WMSContext())
                using (var ts = dc.Database.BeginTransaction())
                {
                    try
                    {
                        if (command.Order_ID != null)
                        {
                            Order order = dc.Orders.FirstOrDefault(prop => prop.ID == command.Order_ID);

                            // if single item changed to active --> put corresponding move command to active
                            if (command.Status == Command.CommandStatus.Active && order?.ERP_ID != null)
                            {
                                var cmdERP = dc.CommandERP.FirstOrDefault(p => p.ID == order.ERP_ID);
                                UpdateERPCommandStatus(cmdERP.ID, CommandERP.CommandERPStatus.Active);
                            }
                            // check if single item (one line (SKU) in order table) finished
                            bool oItemFinished = !dc.Commands
                                                  .Where(prop => prop.Status < Command.CommandStatus.Finished && prop.Order_ID == order.ID)
                                                  .Any();
                            bool oItemCanceled = dc.Commands.Any(prop => prop.Status == Command.CommandStatus.Canceled && prop.Order_ID == order.ID) &&
                                                 !dc.Commands.Any(prop => prop.Status < Command.CommandStatus.Canceled && prop.Order_ID == order.ID);

                            // check if subOrderFinished for one SKU
                            if (oItemFinished || oItemCanceled)
                            {
                                order.Status = oItemFinished ? Order.OrderStatus.Finished : Order.OrderStatus.Canceled;
                                dc.SaveChanges();
                                // check if complete order finished
                                erpid = order.ERP_ID;
                                orderid = order.OrderID;
                            }

                            // One command (pallet) successfully finished
                            if (command.Status == Command.CommandStatus.Finished)
                            {
                            }
                        }
                        ts.Commit();

                        CheckForOrderCompletion(erpid, orderid);

                        return true;
                    }
                    catch (Exception)
                    {
                        ts.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void CheckForOrderCompletion(int? erpid, int orderid)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    bool boolOrdersFinished = !dc.Orders.Any(prop => prop.ERP_ID == erpid && prop.OrderID == orderid &&
                                                             prop.Status < Order.OrderStatus.OnTargetPart);
                    if (boolOrdersFinished && erpid.HasValue)
                    {
                        var order = dc.Orders.FirstOrDefault(prop => prop.ERP_ID == erpid.Value && prop.OrderID == orderid);

                        // set status of corresponding move command
                        var erpcmd = dc.CommandERP.FirstOrDefault(p => p.ID == order.ERP_ID);
                        UpdateERPCommandStatus(erpcmd.ID, CommandERP.CommandERPStatus.Finished);
                    }

                    MoveOrderToHist(erpid, orderid);
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
            }
        }

        public void UpdateERPCommandStatus(int erpid, CommandERP.CommandERPStatus status)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    var cmdERP = dc.CommandERP.Find(erpid);
                    if (cmdERP != null && cmdERP.Status != status)
                    {
                        cmdERP.Status = status;
                        dc.SaveChanges();

                        if (cmdERP.Reference.Contains("MaterialMove"))
                            Task.Run(async () => await ERP_CommandNotify(cmdERP));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void UpdatePlace(string placeID, int TU_ID, int dimensionClass, string changeType)
        {
            try
            {
                {
                    string placeOld = null;
                    using (var dc = new WMSContext())
                    using (var ts = dc.Database.BeginTransaction())
                    {
                        try
                        {
                            string entry = dc.Parameters.Find("Place.IOStation").Value;

                            Place p = dc.Places
                                        .Where(prop => prop.TU_ID == TU_ID)
                                        .FirstOrDefault();
                            placeOld = p?.PlaceID;

                            TU_ID tuid = dc.TU_IDs.Find(TU_ID);
                            if (tuid == null)
                            {
                                dc.TU_IDs.Add(new TU_ID
                                {
                                    ID = TU_ID,
                                    DimensionClass = dimensionClass
                                });
                                Log.AddLog(Log.SeverityEnum.Event, nameof(UpdatePlace), $"TU_IDs add : {TU_ID:d9}");
                            }
                            else
                            {
                                tuid.DimensionClass = dimensionClass;
                            }
                            if (p == null || p.PlaceID != placeID)
                            {
                                if (p != null)
                                    dc.Places.Remove(p);
                                dc.Places.Add(new Place
                                {
                                    PlaceID = placeID,
                                    TU_ID = TU_ID
                                });
                                Log.AddLog(Log.SeverityEnum.Event, nameof(UpdatePlace), $"{placeID},{TU_ID:d9},{changeType}");
                            }
                            dc.SaveChanges();
                            ts.Commit();
                        }
                        catch (Exception e)
                        {
                            ts.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void CancelOrderCommands(DTOOrder orderToCancel)
        {
            // orderToCancel.ID != 0 : only one suborder, 
            // orderToCancel.ID == 0 : all suborders, 
            try
            {
                using (var dc = new WMSContext())
                using (var client = new MFCS_Proxy.WMSClient())
                {
                    // get all suborders
                    var orders = dc.Orders.Where(p => (orderToCancel.ID != 0 && p.ID == orderToCancel.ID) ||
                                                      (orderToCancel.ID == 0 && p.ERP_ID == orderToCancel.ERP_ID && p.OrderID == orderToCancel.OrderID)).ToList();
                    if (orders.Count > 0)
                    {
                        foreach (var o in orders)
                        {
                            if (!dc.Commands.Any(p => p.Order_ID == o.ID))
                            {
                                o.Status = Order.OrderStatus.Canceled;
                                dc.SaveChanges();
                                CheckForOrderCompletion(o.ERP_ID, o.OrderID);
                            }
                            else
                            {
                                var cmds = dc.Commands.Where(p => p.Order_ID == o.ID && p.Status <= Command.CommandStatus.Active);
                                foreach (var c in cmds)
                                {
                                    c.Status = Command.CommandStatus.Canceled;
                                    if (c.Operation == Command.CommandOperation.MoveTray)
                                    {
                                        MFCS_Proxy.DTOCommand[] cs = new MFCS_Proxy.DTOCommand[] { c.ToProxyDTOCommand() };
                                        client.MFCS_Submit(cs);
                                    }
                                    else
                                        UpdateCommand(c.ID, (int)c.Status);
                                }
                            }
                        }
                        CheckForOrderCompletion(orders.FirstOrDefault().ERP_ID, orders.FirstOrDefault().OrderID);
                    }
                    Log.AddLog(Log.SeverityEnum.Event, nameof(CancelOrderCommands), $"Order canceled: {orderToCancel.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
            }
        }
        public void CancelCommand(DTOCommand cmdToCancel)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    var cmd = dc.Commands.Find(cmdToCancel.ID);
                    if (cmd != null)
                    {
                        cmd.Status = Command.CommandStatus.Canceled;
                        if (cmd.Operation == Command.CommandOperation.MoveTray)
                            using (var client = new MFCS_Proxy.WMSClient())
                            {
                                MFCS_Proxy.DTOCommand[] cs = new MFCS_Proxy.DTOCommand[] { cmd.ToProxyDTOCommand() };
                                client.MFCS_Submit(cs);
                            }
                        else
                        {
                            dc.SaveChanges();
                            var order = dc.Orders.FirstOrDefault(p => p.ID == cmd.Order_ID);
                            CheckForOrderCompletion(order?.ERP_ID, order?.OrderID ?? 0);
                        }
                    }
                    Log.AddLog(Log.SeverityEnum.Event, nameof(CancelCommand), $"Command canceled: {cmdToCancel.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
            }
        }

        public void BlockLocations(string locStartsWith, bool block, int reason)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    dc.Database.CommandTimeout = 180;

                    var items = (from pid in dc.PlaceIds
                                 where pid.DimensionClass >= 0 && pid.DimensionClass <= 999 &&
                                       pid.ID.StartsWith(locStartsWith) && ((pid.Status & reason) > 0) != block
                                 join p in dc.Places on pid.ID equals p.PlaceID into grp
                                 from g in grp.DefaultIfEmpty()
                                 select new { PID = pid, TU = g }).ToList();


                    List<int> tuids = new List<int>();

                    if (items != null && items.Count > 0)
                    {
                        // update database
                        if (block)
                        {

                            foreach (var i in items)
                            {
                                i.PID.Status = i.PID.Status | reason;
                                if (i.TU != null)
                                {
                                    i.TU.FK_TU_ID.Blocked = i.TU.FK_TU_ID.Blocked | reason;
                                    tuids.Add(i.TU.TU_ID);
                                }

                            }
                        }
                        else
                        {
                            int mask = int.MaxValue ^ reason;
                            foreach (var i in items)
                            {
                                i.PID.Status = i.PID.Status & mask;
                                if (i.TU != null)
                                {
                                    i.TU.FK_TU_ID.Blocked = i.TU.FK_TU_ID.Blocked & mask;
                                    tuids.Add(i.TU.TU_ID);
                                }
                            }
                        }

                        // inform MFCS
                        using (var client = new MFCS_Proxy.WMSClient())
                        {
                            string[] la = new string[] { locStartsWith };
                            if (block)
                                client.MFCS_PlaceBlock(la, 0);
                            else
                                client.MFCS_PlaceUnblock(la, 0);
                        }
                        string blocked = block ? "blocked" : "released";

                        Log.AddLog(Log.SeverityEnum.Event, nameof(BlockLocations), $"Locations {blocked}: {locStartsWith}* ({reason})");
                        dc.SaveChanges();

                        // inform ERP
                    }
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
            }
        }
        public void BlockTU(int TUID, bool block, int reason)
        {
            try
            {
                List<int> tuids = new List<int>();

                using (var dc = new WMSContext())
                {
                    var items = (from tuid in dc.TU_IDs
                                 where tuid.ID == TUID
                                 select tuid).ToList();
                    if (block)
                    {
                        foreach (var i in items)
                        {
                            i.Blocked = i.Blocked | reason;
                            tuids.Add(i.ID);
                        }
                    }
                    else
                    {
                        int mask = int.MaxValue ^ reason;
                        foreach (var i in items)
                        {
                            i.Blocked = i.Blocked & mask;
                            tuids.Add(i.ID);
                        }
                    }
                    string blocked = block ? "blocked" : "released";
                    Log.AddLog(Log.SeverityEnum.Event, nameof(BlockTU), $"TU {blocked}: {TUID}* ({reason})");
                    dc.SaveChanges();

                    // inform ERP
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex);
                SimpleLog.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
            }
        }

        public void SyncDatabase(List<PlaceDiff> list, string user)
        {
            try
            {
                using (var dcw = new WMSContext())
                {

                    string exit = dcw.Parameters.Find("Place.OutOfWarehouse").Value;

                    foreach (var l in list)
                    {
                        var mid = dcw.TU_IDs.FirstOrDefault(p => p.ID == l.TUID);
                        if (mid == null)
                        {
                            var tuid = new TU_ID { ID = l.TUID, DimensionClass = l.DimensionMFCS, Blocked = 0 };
                            dcw.TU_IDs.Add(tuid);
                            Log.AddLog(Log.SeverityEnum.Event, $"nameof(SyncDatabase), {user}", $"Update places WMS, add TUID: {tuid.ToString()}|");
                        }
                        else
                            mid.DimensionClass = l.DimensionMFCS;
                        dcw.SaveChanges();
                        var place = dcw.Places.FirstOrDefault(pp => pp.TU_ID == l.TUID);
                        // delete
                        if (place != null && l.PlaceMFCS == null)
                        {
                            dcw.Places.Remove(place);
                            dcw.SaveChanges();
                            UpdatePlace(exit, l.TUID, l.DimensionWMS, "MOVE");
                            Log.AddLog(Log.SeverityEnum.Event, $"{nameof(SyncDatabase)}, {user}", $"Update places WMS, remove TU: {l.TUID:d9}, {place.ToString()}");
                        }
                        // move
                        else if (place != null && l.PlaceMFCS != null)
                        {
                            dcw.Places.Remove(place);
                            var pl = new Place { TU_ID = l.TUID, PlaceID = l.PlaceMFCS, Time = DateTime.Now };
                            dcw.Places.Add(pl);
                            dcw.SaveChanges();
                            UpdatePlace(l.PlaceMFCS, l.TUID, l.DimensionMFCS, "MOVE");
                            Log.AddLog(Log.SeverityEnum.Event, $"{nameof(SyncDatabase)}, {user}", $"Update places WMS, rebook TU: {l.TUID:d9}, {place.ToString()}");
                        }
                        // create
                        else if (place == null && l.PlaceMFCS != null)
                        {
                            var pl = new Place { TU_ID = l.TUID, PlaceID = l.PlaceMFCS, Time = DateTime.Now };
                            dcw.Places.Add(pl);
                            dcw.SaveChanges();
                            UpdatePlace(l.PlaceMFCS, l.TUID, l.DimensionMFCS, "CREATE");
                            Log.AddLog(Log.SeverityEnum.Event, $"{nameof(SyncDatabase)}, {user}", $"Update places WMS, create TU: {pl.ToString()}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task AddTUs(List<TU> tus)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    dc.TUs.AddRange(tus);
                    dc.SaveChanges();

                    foreach(var t in tus)
                    {
                        Log.AddLog(Log.SeverityEnum.Event, "UI", $"Drop completed: {t.Box_ID} to {t.TU_ID}");
                        await ERP_BoxNotify(BoxNotifyType.Drop, t.Box_ID, t.TU_ID);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task DeleteTU(TU tu)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    var ldel = dc.TUs.Where(p => p.Box_ID == tu.Box_ID).ToList();
                    dc.TUs.RemoveRange(ldel);

                    dc.SaveChanges();

                    foreach (var ld in ldel)
                    {
                        Log.AddLog(Log.SeverityEnum.Event, "UI", $"Pick completed: {ld.Box_ID} from {ld.TU_ID}");
                        await ERP_BoxNotify(BoxNotifyType.Pick, ld.Box_ID, ld.TU_ID);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
        public async Task ERP_BoxNotify(BoxNotifyType ntype, string box, int? tuid)
        {
            try
            {
                bool.TryParse(ConfigurationManager.AppSettings["ERPpresent"], out bool erpPresent);

                using (var dc = new WMSContext())
                {
                    // first we create a command to get an ID
                    CommandERP cmdERP = new CommandERP
                    {
                        ERP_ID = 0,
                        Command = "<MaterialNotify/>",
                        Reference = "MaterialNotify",
                        Status = CommandERP.CommandERPStatus.NotActive,
                        Time = DateTime.Now,
                        LastChange = DateTime.Now
                    };
                    dc.CommandERP.Add(cmdERP);
                    dc.SaveChanges();

                    switch (ntype)
                    {
                        case BoxNotifyType.Drop:
                            Xml.XmlMaterialStored xmlBoxStored = new Xml.XmlMaterialStored
                            {
                                DocumentID = cmdERP.ID,
                                Commands = new Command[] {
                                    new Command {
                                        Box_ID = box,
                                        TU_ID = tuid??0 }}
                            };
                            cmdERP.Command = xmlBoxStored.BuildXml();
                            cmdERP.Reference = xmlBoxStored.Reference();
                            break;
                        case BoxNotifyType.Pick:
                            Xml.XmlMaterialRetrieved xmlBoxRetrieved = new Xml.XmlMaterialRetrieved
                            {
                                DocumentID = cmdERP.ID,
                                Commands = new Command[] {
                                    new Command {
                                        Box_ID = box,
                                        TU_ID = tuid??0 }}
                            };
                            cmdERP.Command = xmlBoxRetrieved.BuildXml();
                            cmdERP.Reference = xmlBoxRetrieved.Reference();
                            break;
                        default:
                            Xml.XmlMaterialEntry xmlBoxEntry = new Xml.XmlMaterialEntry
                            {
                                DocumentID = cmdERP.ID,
                                Commands = new Command[] {
                                    new Command {
                                        Box_ID = box }}
                            };
                            cmdERP.Command = xmlBoxEntry.BuildXml();
                            cmdERP.Reference = xmlBoxEntry.Reference();
                            break;
                    }

                    // update command
                    cmdERP.LastChange = DateTime.Now;
                    Log.AddLog(Log.SeverityEnum.Event, nameof(ERP_BoxNotify), $"CommandERP created : {cmdERP.Reference}");
                    dc.SaveChanges();

                    using (ERP_Proxy.SBWSSoapClient proxyERP = new ERP_Proxy.SBWSSoapClient())
                    {
                        string reply = "";
                        try
                        {
                            if (erpPresent)
                            {
                                var retVal = await proxyERP.WritePickToDocumentAsync(_erpUser, _erpPwd, _erpCode, cmdERP.Command, "");
                                reply = $"<reply>\n\t<type>{retVal[0].ResultType}</type>\n\t<string>{retVal[0].ResultString}</string>\n</reply>";

                                cmdERP.Status = (retVal[0].ResultType == ERP_Proxy.clsERBelgeSonucTip.OK) ?
                                                CommandERP.CommandERPStatus.Finished : CommandERP.CommandERPStatus.Error;
                            }
                            else
                            {
                                reply = $"<reply>NOERP</reply>";
                                cmdERP.Status = CommandERP.CommandERPStatus.Finished;
                            }

                        }
                        catch (Exception ex)
                        {
                            reply = $"<reply>\n\t<type>{1}</type>\n\t<string>{ex.Message}</string></reply>";
                            Log.AddException(ex);
                            SimpleLog.AddException(ex, nameof(Model));
                            Debug.WriteLine(ex.Message);
                            cmdERP.Status = CommandERP.CommandERPStatus.Error;
                        }
                        cmdERP.Command = $"<!-- CALL -->\n{cmdERP.Command}\n\n<!-- REPLY -->\n{reply}\n";
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }

        public async Task ERP_CommandNotify(CommandERP cmd)
        {
            try
            {
                bool.TryParse(ConfigurationManager.AppSettings["ERPpresent"], out bool erpPresent);

                using (var dc = new WMSContext())
                { 
                    // first we create a command to get an ID
                    CommandERP cmdERP = new CommandERP
                    {
                        ERP_ID = 0,
                        Command = "<MaterialMoveStatus/>",
                        Reference = "MaterialMoveStatus",
                        Status = CommandERP.CommandERPStatus.NotActive,
                        Time = DateTime.Now,
                        LastChange = DateTime.Now
                    };
                    dc.CommandERP.Add(cmdERP);
                    dc.SaveChanges();
                    // generate xml
                    Xml.XmlMaterialMoveStatus xmlStatus = new Xml.XmlMaterialMoveStatus { DocumentID = cmdERP.ID, Command = cmd };
                    cmdERP.Command = xmlStatus.BuildXml();
                    cmdERP.Reference = xmlStatus.Reference();
                    cmdERP.LastChange = DateTime.Now;
                    cmdERP.Status = CommandERP.CommandERPStatus.Finished;
                    dc.SaveChanges();

                    Log.AddLog(Log.SeverityEnum.Event, nameof(CheckForOrderCompletion), $"CommandERP created : {cmdERP.Reference}");

                    // notify ERP
                    using (ERP_Proxy.SBWSSoapClient proxyERP = new ERP_Proxy.SBWSSoapClient())
                    {
                        string reply = "";
                        try
                        {
                            if (erpPresent)
                            {
                                var retVal = await proxyERP.WritePickToDocumentAsync(_erpUser, _erpPwd, _erpCode, cmdERP.Command, "");
                                reply = $"<reply>\n\t<type>{retVal[0].ResultType}</type>\n\t<string>{retVal[0].ResultString}</string>\n</reply>";

                                cmdERP.Status = (retVal[0].ResultType == ERP_Proxy.clsERBelgeSonucTip.OK) ?
                                                CommandERP.CommandERPStatus.Finished : CommandERP.CommandERPStatus.Error;
                            }
                            else
                            {
                                reply = $"<reply>NOERP</reply>";
                                cmdERP.Status = CommandERP.CommandERPStatus.Finished;
                            }

                        }
                        catch (Exception ex)
                        {
                            reply = $"<reply>\n\t<type>{1}</type>\n\t<string>{ex.Message}</string></reply>";
                            Log.AddException(ex);
                            SimpleLog.AddException(ex, nameof(Model));
                            Debug.WriteLine(ex.Message);
                            cmdERP.Status = CommandERP.CommandERPStatus.Error;
                        }
                        cmdERP.Command = $"<!-- CALL -->\n{cmdERP.Command}\n\n<!-- REPLY -->\n{reply}\n";
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0}.{1}: {2}", this.GetType().Name, (new StackTrace()).GetFrame(0).GetMethod().Name, e.Message));
            }
        }
    }
}
