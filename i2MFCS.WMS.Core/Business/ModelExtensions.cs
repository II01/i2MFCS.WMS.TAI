using i2MFCS.WMS.Database.DTO;
using i2MFCS.WMS.Database.Tables;
using SimpleLogs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Linq.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static i2MFCS.WMS.Database.Tables.Command;

namespace i2MFCS.WMS.Core.Business
{
    public static class ModelExtensions
    {
        public static Random Random = new Random();

        public static IEnumerable<Command> ToCommands(this IEnumerable<Place> list, int orderID, string target)
        {
            foreach (Place p in list)
                yield return new Command { TU_ID = p.TU_ID, Source = p.PlaceID, Target = target, Order_ID = orderID, Status = 0, LastChange = DateTime.Now };
        }


        /// <summary>
        /// Optimised search frok SKUID,Batch,Qty order demands -> DTOCommnand
        /// </summary>
        /// <param name="dtoOrders"></param>
        /// <returns></returns>
        public static IEnumerable<DTOCommand> DTOOrderToDTOCommand(this IEnumerable<DTOOrder> dtoOrders)
        {
            using (var dc = new WMSContext())
            {
                return dtoOrders
                    .SelectMany(p =>
                            p.Operation == Order.OrderOperation.Move ?
                                new DTOCommand[]
                                {
                                    new DTOCommand
                                    {
                                        Order_ID = p.ID,
                                        TU_ID = p.TU_ID,
                                        Source = dc.Places.Find(p.TU_ID).PlaceID,
                                        Target = p.Destination,
                                        Operation = Command.CommandOperation.Move,
                                        Status = Command.CommandStatus.NotActive
                                    },
                                    new DTOCommand
                                    {
                                        Order_ID = p.ID,
                                        TU_ID = p.TU_ID,
                                        Source = dc.Places.Find(p.TU_ID).PlaceID,
                                        Target = p.Destination,
                                        Operation = Command.CommandOperation.Confirm,
                                        Status = Command.CommandStatus.NotActive
                                    }
                                } 
                                :
                                new DTOCommand[]
                                {
                                    new DTOCommand
                                    {
                                        Order_ID = p.ID,
                                        TU_ID = p.TU_ID,
                                        Source = p.Destination,
                                        Target = p.Destination,
                                        Operation = (CommandOperation)p.Operation,
                                        Status = Command.CommandStatus.NotActive
                                    }
                                }
                    );
            }
        }

        /// <summary>
        /// DTOOrder is connected only to one  DTOCommand 
        /// Order can be connected to many DTOCommands
        /// Order 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<DTOOrder> OrderToDTOOrders(this IEnumerable<Order> orders)
        {
            return orders
                    .GroupBy(
                        (by) => by.TU_ID,
                        (key, group) => new
                        {
                            tuid = key,
                            boxes = String.Join("|", group.Select(b => b.Box_ID)),
                            firstorder = group.FirstOrDefault(),
                            group
                        })
                    .SelectMany(p =>
                        (
                            new DTOOrder[]
                            {
                                new DTOOrder
                                {
                                    ID = p.firstorder.ID,
                                    ERP_ID = p.firstorder.ERP_ID,
                                    OrderID = p.firstorder.OrderID,
                                    SubOrderID = p.firstorder.SubOrderID,
                                    SubOrderName = p.firstorder.SubOrderName,
                                    Boxes = p.boxes,
                                    TU_ID = p.tuid,
                                    Source = p.firstorder.Operation >= Order.OrderOperation.RetrieveTray ? "W" : p.firstorder.Destination,
                                    Destination = p.firstorder.Operation >= Order.OrderOperation.RetrieveTray ? p.firstorder.Destination : "W",
                                    Operation = Order.OrderOperation.Move
                                }
                            }
                        )
                        .Union
                        (
                            p.group.Select(p1 => new DTOOrder
                            {
                                ID = p1.ID,
                                ERP_ID = p1.ERP_ID,
                                OrderID = p1.OrderID,
                                Source = p1.Destination,
                                Destination = p1.Destination,
                                SubOrderID = p1.SubOrderID,
                                SubOrderName = p1.SubOrderName,
                                TU_ID = p1.TU_ID,
                                Operation = p1.Operation
                            })
                        )
                    );                    
        }


        public static IEnumerable<Command> ToCommand( this IEnumerable<DTOCommand> cmds)
        {
            return cmds.Select(p => p.ToCommand());
        }

        public static Command ToCommand(this DTOCommand cmd)
        {
            return new Command
            {
                Order_ID = cmd.Order_ID,
                Source = cmd.Source,
                TU_ID = cmd.TU_ID,
                Status = cmd.Status,
                Target = cmd.Target == "W" ? cmd.GetRandomPlace(null) : cmd.Target,
                LastChange = cmd.LastChange
            };
        }

        public static MFCS_Proxy.DTOCommand ToProxyDTOCommand(this Command cmd)
        {
            return new MFCS_Proxy.DTOCommand
            {
                Order_ID = cmd.ID,
                Source = cmd.Source,
                Status = (int)cmd.Status,
                Target = cmd.Target,
                Time = cmd.Time,
                TU_ID = cmd.TU_ID
            };
        }

        public static IEnumerable<MFCS_Proxy.DTOCommand> ToProxyDTOCommand(this IEnumerable<Command> cmds)
        {
            return cmds.Select(p => new MFCS_Proxy.DTOCommand
            {
                Order_ID = p.ID,
                Source = p.Source,
                Status = (int)p.Status,
                Target = p.Target,
                Time = p.Time,
                TU_ID = p.TU_ID
            });
        }

        /// <summary>
        /// Get random place 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static string GetRandomPlace(this DTOCommand command, List<string> forbidden)
        {
            using (var dc = new WMSContext())
            {
                int frequencyClass = 0;

                var type = dc.TU_IDs
                            .FirstOrDefault(prop => prop.ID == command.TU_ID);
                var tu = dc.TUs
                         .FirstOrDefault(prop => prop.TU_ID == command.TU_ID);
                if (tu != null)
                {
//                    var skuid = dc.SKU_IDs
//                                .FirstOrDefault(prop => prop.ID == tu.SKU_ID);
//                    frequencyClass = skuid.FrequencyClass;
                }
                var source = dc.PlaceIds
                            .FirstOrDefault(prop => prop.ID == command.Source);

                var free =
                    dc.PlaceIds
                    .Where(p => !p.FK_Places.Any()
                                && p.DimensionClass == type.DimensionClass
                                && p.Status == 0
                                && (command.Source.StartsWith("T") ||
                                    (p.ID.Substring(0, 3) == command.Source.Substring(0, 3)))
                                && !p.FK_Target_Commands.Any(prop => prop.Status < Command.CommandStatus.Canceled)
                                && !forbidden.Any(prop => p.ID == prop));

                Log.AddLog(Log.SeverityEnum.Event, nameof(GetRandomPlace), $"CreateInputCommand {command.TU_ID}: random 1");

                int count = free
                            .Where(p => p.FrequencyClass == frequencyClass)
                            .OrderBy(p => p.ID)
                            .Count();

                Log.AddLog(Log.SeverityEnum.Event, nameof(GetRandomPlace), $"CreateInputCommand {command.TU_ID}: random 1b");

                if (count > 0)
                {
                    Log.AddLog(Log.SeverityEnum.Event, nameof(GetRandomPlace), $"CreateInputCommand {command.TU_ID}: random 3");
                    return free.Skip(Random.Next(count)).FirstOrDefault().ID;
                }
                else
                    throw new Exception($"Warehouse is full (demand from {command.Source})");
            }
        }
    }
}


