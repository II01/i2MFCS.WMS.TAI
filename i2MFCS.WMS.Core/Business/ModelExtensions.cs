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
        /// DTOOrder is connected only to one  DTOCommand 
        /// Order can be connected to many DTOCommands
        /// Order 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static IEnumerable<DTOOrder> OrderToDTOOrders(this IEnumerable<Order> orders)
        {
            string whloc;
            string outloc;
            using (var dc = new WMSContext())
            {
                whloc = dc.Parameters.Find("Place.WarehouseAny").Value;
                outloc = dc.Parameters.Find("Place.OutOfWarehouse").Value;
            }
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
                                TU_ID = p.tuid,
                                Boxes = p.boxes,
                                Source = p.firstorder.Operation > Order.OrderOperation.MoveTray ? whloc : p.firstorder.Destination,
                                Destination = p.firstorder.Operation < Order.OrderOperation.MoveTray ? whloc : p.firstorder.Destination,
                                Operation = Order.OrderOperation.MoveTray
                            }
                        }
                    )
                    .Union
                    (
                        p.group.Select(pp => new DTOOrder
                                                {
                                                    ID = pp.ID,
                                                    ERP_ID = pp.ERP_ID,
                                                    OrderID = pp.OrderID,
                                                    SubOrderID = pp.SubOrderID,
                                                    SubOrderName = pp.SubOrderName,
                                                    TU_ID = pp.TU_ID,
                                                    Boxes = pp.Box_ID,
                                                    Source = (pp.Operation == Order.OrderOperation.StoreTray || pp.Operation == Order.OrderOperation.DropBox) ? outloc : pp.Destination,
                                                    Destination = (pp.Operation == Order.OrderOperation.RetrieveTray || pp.Operation == Order.OrderOperation.PickBox) ? outloc : pp.Destination,
                                                    Operation = pp.Operation
                                                })
                    )
                );
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
                bool store = dtoOrders.FirstOrDefault(p => p.Operation == Order.OrderOperation.StoreTray) != null;
                return dtoOrders
                    .SelectMany(p =>
                            p.Operation == Order.OrderOperation.MoveTray ?
                                new DTOCommand[]
                                {
                                    new DTOCommand
                                    {
                                        Order_ID = p.ID,
                                        TU_ID = p.TU_ID,
                                        Box_ID = "-",
                                        Source = p.Source,
                                        Target = p.Destination,
                                        Operation = Command.CommandOperation.MoveTray,
                                        Status = Command.CommandStatus.NotActive,
                                        LastChange = DateTime.Now
                                    },
                                    new DTOCommand
                                    {
                                        Order_ID = p.ID,
                                        TU_ID = p.TU_ID,
                                        Box_ID = "-",
                                        Source = p.Source.StartsWith("T") ? p.Source : p.Destination,
                                        Target = p.Source.StartsWith("T") ? p.Source : p.Destination,
                                        Operation = store ? Command.CommandOperation.ConfirmStore :  Command.CommandOperation.ConfirmFinish,
                                        Status = Command.CommandStatus.NotActive,
                                        LastChange = DateTime.Now
                                    }
                                }
                                :
                                new DTOCommand[]
                                {
                                    new DTOCommand
                                    {
                                        Order_ID = p.ID,
                                        TU_ID = p.TU_ID,
                                        Box_ID = p.Boxes,
                                        Source = p.Source,
                                        Target = p.Destination,
                                        Operation = (CommandOperation)p.Operation,
                                        Status = Command.CommandStatus.NotActive,
                                        LastChange = DateTime.Now
                                    }
                                }
                    ).ToList();
            }
        }

        public static IEnumerable<Command> ToCommand( this IEnumerable<DTOCommand> cmds)
        {
            return cmds.Select(p => p.ToCommand());
        }

        public static Command ToCommand(this DTOCommand cmd)
        {
            var c= new Command
                    {
                        Order_ID = cmd.Order_ID,
                        Operation = cmd.Operation,
                        TU_ID = cmd.TU_ID,
                        Box_ID = cmd.Box_ID,
                        Source = cmd.Source.StartsWith("W:any") && cmd.Operation == CommandOperation.MoveTray ? cmd.GetSourcePlace() : cmd.Source,
                        Target = cmd.Target.StartsWith("W:any") && cmd.Operation == CommandOperation.MoveTray ? cmd.GetRandomPlace(new List<string>()) : cmd.Target,
                        Status = cmd.Status,
                        LastChange = cmd.LastChange
                    };
            if (c.Source == c.Target && c.Operation == CommandOperation.MoveTray)
                c.Status = CommandStatus.Finished;

            return c;
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
                var type = dc.TU_IDs
                            .FirstOrDefault(p => p.ID == command.TU_ID);

                var free =
                    dc.PlaceIds
                    .Where(p => p.ID.StartsWith("W:")
                                && !p.FK_Places.Any()
                                && p.DimensionClass >= type.DimensionClass
                                && p.Status == 0
                                && (command.Source.StartsWith("T") ||
                                    (p.ID.Substring(0, 3) == command.Source.Substring(0, 3)))
                                && !p.FK_Target_Commands.Any(prop => prop.Status < Command.CommandStatus.Canceled)
                                && !forbidden.Any(pp => pp == p.ID))
                    .OrderBy(p => p.DimensionClass)
                    .ThenBy(p => p.ID);

                int count = free
                            .Where(p => p.DimensionClass == type.DimensionClass)
                            .Count();

                if (count == 0)
                    count = free.Count();

                if (count > 0)
                    return free.Skip(Random.Next(count)).FirstOrDefault().ID;
                else
                    throw new Exception($"Warehouse is full (demand from {command.Source})");
            }
        }


        public static string GetSourcePlace(this DTOCommand command)
        {
            using (var dc = new WMSContext())
            {
                var place = dc.Places.First(pp => pp.TU_ID == command.TU_ID).PlaceID;

                if (place == null)
                    throw new Exception($"No Source found for {command.TU_ID})");

                return place;
            }
        }

    }
}


