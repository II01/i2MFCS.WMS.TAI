using i2MFCS.WMS.Database.DTO;
using i2MFCS.WMS.Database.Tables;
using SimpleLogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace i2MFCS.WMS.Core.Business
{

    public class Model
    {
        private static Random Random = new Random();
        private static object _lockOperation = new Random();
        private static object _lockSingleton = new object();
        private static Model _singleton = null;

        private Timer _timer;
        private SimulateERP _simulateERP;

        public Model()
        {
            _timer = new Timer(ActionOnTimer,null,1000,2000);
            _simulateERP = new SimulateERP();
        }

        public void ActionOnTimer(object state)
        {
            lock (this)
            {
                try
                {
                    // _simulateERP.SimulateIncomingTUs("T014", "MAT03", "BATCH04", 5);
                    CreateInputCommand();
                    CreateOutputCommands();
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
        public void CreateOutputCommands()
        {
            try
            {
                using (var dc = new WMSContext())
                using (var ts = dc.Database.BeginTransaction())
                {
                    DateTime now = DateTime.Now;
                    var findOrders = dc.Orders
                            .Where (p => p.Status < Order.OrderStatus.Canceled)
                            .GroupBy(
                            (by) => new { by.Destination }
                            )
                            .Where(p => !p.Any(p1 => p1.Status > Order.OrderStatus.NotActive && p1.Status < Order.OrderStatus.Canceled))
                            .Select(group => new
                            {
                                Key = group.Key,
                                Suborders = group
                                           .Where(p => p.Status == Order.OrderStatus.NotActive)
                                           .GroupBy(
                                           (by) => new { by.OrderID, by.SubOrderID }
                                           )
                                           .Where(p => p.FirstOrDefault().ReleaseTime < now)
                                           .FirstOrDefault()
                            })
                            .SelectMany(p => p.Suborders)
                            .ToList();

                    /// Alternative faster solution
                    /// Create DTOOrders from Orders
                    List<DTOOrder> dtoOrders =
                        findOrders
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
                                         .Join(dc.Places,
                                                command => command.Source.Substring(0, 10) + ":1",
                                                neighbour => neighbour.PlaceID,
                                                (command, neighbour) => new { Command = command, Neighbour = neighbour }
                                         )
                                         .Where(prop => !prop.Neighbour.FK_PlaceID.FK_Source_Commands.Any())
                                         .Select(prop => prop.Command)
                                         .ToList();

                        List<DTOCommand> transferCmd = transferProblemCmd
                                        .TakeNeighbour()
                                        .MoveToBrotherOrFree()
                                        .ToList();

                        dc.SaveChanges();
                        var commands = new List<Command>();
                        foreach (var cmd in cmdSortedFromOne)
                        {
                            int i = transferProblemCmd.IndexOf(cmd);
                            if (i != -1)
                            {
                                Debug.WriteLine($"Transfer command : {transferCmd[i].ToString()}");
                                SimpleLog.AddLog(SimpleLog.Severity.EVENT, nameof(Model), $"Transfer command : {transferCmd[i].ToString()}", "");
                                Log.AddLog(Log.SeverityEnum.Event, nameof(CreateOutputCommands), $"Transfer command : {transferCmd[i].ToString()}");
                                commands.Add(transferCmd[i].ToCommand());
                            }
                            Debug.WriteLine($"Output command : {cmd.ToString()}");
                            SimpleLog.AddLog(SimpleLog.Severity.EVENT, nameof(Model), $"Output command : {cmd.ToString()}", "");
                            Log.AddLog(Log.SeverityEnum.Event, nameof(CreateOutputCommands), $"Output command : {cmd.ToString()}");
                            commands.Add(cmd.ToCommand());
                        }
                        dc.Commands.AddRange(commands);
                        // notify ERP about changes

                        dc.SaveChanges();
                        using (MFCS_Proxy.WMSClient proxy = new MFCS_Proxy.WMSClient())
                        {
                            proxy.MFCS_Submit(commands.ToProxyDTOCommand().ToArray());
                        }
                        ts.Commit();
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

        public void CreateInputCommand()
        {
            try
            {
                using (var dc = new WMSContext())
                using (var ts = dc.Database.BeginTransaction())
                {
                    string source = dc.Parameters.Find("InputCommand.Place").Value;
                    List<string> forbidden = new List<string>();
                    Place place = dc.Places.FirstOrDefault(prop => prop.PlaceID == source);
                    if (place != null)
                    {
                        TU tu = dc.TUs.FirstOrDefault(prop => prop.TU_ID == place.TU_ID);
                        if (tu == null)
                        {
                            // add ERP notitication here
                            var xmlErp = new Core.Xml.XmlWriteMovementToHB
                            {
                                DocumentID = 0,
                                DocumentType = nameof(Xml.XmlWriteMovementToHB),
                                TU_IDs = new int[] { place.TU_ID }
                            };

                            string searchFor = xmlErp.Reference();
                            if (dc.CommandERP.FirstOrDefault(prop => prop.Reference == searchFor) == null)
                            {
                                CommandERP erpCmd = new CommandERP
                                {
                                    ERP_ID = 0,
                                    Command = xmlErp.BuildXml(),
                                    Reference = xmlErp.Reference(),
                                    Status = 0,
                                };
                                dc.CommandERP.Add(erpCmd);
                                dc.SaveChanges();
                                xmlErp.DocumentID = erpCmd.ID;
                                erpCmd.Command = xmlErp.BuildXml();
                                dc.SaveChanges();
                                // make call to ERP via WCF
                                using (ERP_Proxy.SBWSSoapClient proxyERP = new ERP_Proxy.SBWSSoapClient())
                                {
                                    var retVal = proxyERP.WriteMovementToSBWithBarcode("a", "b", 123, erpCmd.Command, "e");
                                    //retVal[0].ResultType;
                                    //retVal[0].ResultString;
                                }
                                ts.Commit();
                                Log.AddLog(Log.SeverityEnum.Event, nameof(CreateInputCommand), $"CommandERP created : {erpCmd.Reference}");
                            }
                        }
                        else if (place != null && !place.FK_PlaceID.FK_Source_Commands.Any(prop => prop.Status < Command.CommandStatus.Canceled && prop.TU_ID == place.TU_ID)
                            && tu != null)
                        {
                            var cmd = new DTOCommand
                            {
                                Order_ID = null,
                                TU_ID = place.TU_ID,
                                Source = "T014",
                                Target = null,
                                Status = 0
                            };
                            string brother = cmd.FindBrotherOnDepth2();
                            if (brother != null)
                                cmd.Target = brother.Substring(0, 10) + ":1";
                            else
                                cmd.Target = cmd.GetRandomPlace(forbidden); 
                            Command c = cmd.ToCommand();
                            dc.Commands.Add(c);
                            dc.SaveChanges();
                            using (MFCS_Proxy.WMSClient proxy = new MFCS_Proxy.WMSClient())
                            {
                                MFCS_Proxy.DTOCommand[] cs = new MFCS_Proxy.DTOCommand[] { c.ToProxyDTOCommand() };
                                proxy.MFCS_Submit(cs);
                            }
                            ts.Commit();
                            Debug.WriteLine($"Input command for {source} crated : {cmd.ToString()}");
                            SimpleLog.AddLog(SimpleLog.Severity.EVENT, nameof(Model), $"Command created : {c.ToString()}", "");
                            Log.AddLog(Log.SeverityEnum.Event, nameof(CreateInputCommand), $"Command created : {c.ToString()}");
                        }
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


        public bool CommandChangeNotifyERP(Command command)
        {
            try
            {
                using (var dc = new WMSContext())
                using (var ts = dc.Database.BeginTransaction())
                {

                    if (command.Order_ID != null)
                    {
                        // check if single item finished
                        bool oItemFinished = !dc.Commands
                                        .Where(prop => prop.Status < Command.CommandStatus.Finished && prop.ID == command.Order_ID)
                                        .Any();

                        // check if subOrderFinished
                        Order order = dc.Orders.FirstOrDefault(prop => prop.ID == command.Order_ID);
                        if (oItemFinished)
                        {
                            order.Status = Order.OrderStatus.OnTarget;
                            if (order.ERP_ID.HasValue)
                            {
                                Xml.XmlReadERPCommandStatus xmlStatus = new Xml.XmlReadERPCommandStatus
                                {
                                    OrderToReport = new Order[] { order }
                                };
                                CommandERP cmdERP1 = new CommandERP
                                {
                                    ERP_ID = order.ERP_ID.Value,
                                    Command = xmlStatus.BuildXml(),
                                    Reference = xmlStatus.Reference()
                                };
                                dc.CommandERP.Add(cmdERP1);
                                Log.AddLog(Log.SeverityEnum.Event, nameof(CommandChangeNotifyERP), $"CommandERP created : {cmdERP1.Reference}");
                            }
                            dc.SaveChanges();
                            // TODO-WMS call XmlReadERPCommandStatus via WCF
                        }
                        Xml.XmlWritePickToDocument xmlPickDocument = new Xml.XmlWritePickToDocument
                        {
                            DocumentID = order.ERP_ID.HasValue ? order.ERP_ID.Value : 0,
                            Commands = new Command[] { command }
                        };
                        CommandERP cmdERP;
                        dc.CommandERP.Add(cmdERP = new CommandERP
                        {
                            ERP_ID = order.ERP_ID.HasValue ? order.ERP_ID.Value : 0,
                            Command = xmlPickDocument.BuildXml(), 
                            Reference = xmlPickDocument.Reference()
                        });
                        Log.AddLog(Log.SeverityEnum.Event, nameof(CommandChangeNotifyERP), $"CommandERP created : {cmdERP.Reference}");
                        dc.SaveChanges();
                        // TODO-WMS call XMlWritePickToDocument
                        using (ERP_Proxy.SBWSSoapClient proxyERP = new ERP_Proxy.SBWSSoapClient())
                        {
                            var retVal = proxyERP.WritePickToDocument("a", "b", 0, "d", "e");
                            //retVal[0].ResultType;
                            //retVal[0].ResultString;
                        }
                    }
                    ts.Commit();
                    return true;
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


        public void MFCSUpdatePlace(string placeID, int TU_ID)
        {
            try
            {
//                lock (this)
                {
                    using (var dc = new WMSContext())
                    using (var ts = dc.Database.BeginTransaction())
                    {
                        Place p = dc.Places
                                    .Where(prop => prop.TU_ID == TU_ID)
                                    .FirstOrDefault();
                        TU_ID tuid = dc.TU_IDs.Find(TU_ID);
                        if (tuid == null)
                        {
                            dc.TU_IDs.Add(new TU_ID
                            {
                                ID = TU_ID
                            });
                            Log.AddLog(Log.SeverityEnum.Event, nameof(MFCSUpdatePlace), $"TU_IDs add : {TU_ID}");
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
                            Log.AddLog(Log.SeverityEnum.Event, nameof(MFCSUpdatePlace), $"{placeID},{TU_ID}");
                        }
                        dc.SaveChanges();
                        ts.Commit();
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


        public void MFCSUpdateCommand(int id, int status)
        {
            try
            {
//                lock (this)
                {
                    using (var dc = new WMSContext())
                    {
                        var cmd = dc.Commands.Find(id);
                        Command.CommandStatus oldS = cmd.Status;
                        cmd.Status = (Command.CommandStatus) status;
                        dc.SaveChanges();
                        Log.AddLog(Log.SeverityEnum.Event, nameof(MFCSUpdateCommand), $"{id}, {status}");
                        if (oldS != cmd.Status && cmd.Status >= Command.CommandStatus.Finished )
                            CommandChangeNotifyERP(cmd);
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

        public void MFCSDestinationEmptied(string place)
        {
            try
            {
//                lock (this)
                {
                    using (var dc = new WMSContext())
                    {
                       var orders = dc.Orders
                                .Where(prop => prop.Destination.StartsWith(place)
                                       && ((prop.Status == Order.OrderStatus.OnTarget) || (prop.Status == Order.OrderStatus.WaitForTakeoff)))
                                       .ToList();
                        orders.ForEach(prop => prop.Status = Order.OrderStatus.Finished);
                        dc.SaveChanges();
                        Log.AddLog(Log.SeverityEnum.Event, nameof(MFCSDestinationEmptied), $"{string.Join(",",orders.Select(p=>p.ID))}");
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
    }
}
