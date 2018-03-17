using i2MFCS.WMS.Database.DTO;
using i2MFCS.WMS.Database.Tables;
using SimpleLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Core.Business
{
    public static class ModelExtensions
    {
        public static Random Random = new Random();

        public static IEnumerable<Command> ToCommands(this IEnumerable<Place> list, int orderID, string target)
        {
            foreach (Place p in list)
                yield return new Command { TU_ID = p.TU_ID, Source = p.PlaceID, Target = target, Order_ID = orderID, Status = 0 };
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
                var res = dtoOrders
                .GroupBy(
                    (by) => new { by.SKU_ID , by.SKU_Batch, by.SKU_Qty },
                    (key, dtoOrderGroup) => new
                    {
                        Key = key,
                        Num = dtoOrderGroup.Count(),
                        DTOOrders =
                           dtoOrderGroup
                           .ToList(),
                        Place =
                           dc.TUs
                           .Where(prop => prop.SKU_ID == key.SKU_ID && prop.Batch == key.SKU_Batch && prop.Qty == key.SKU_Qty)
                           .Join( dc.Places,
                              (tu) => tu.TU_ID,
                              (place) => place.TU_ID,
                              (tu,place) => new {TU=tu, Place=place}
                            )
                            .Where ( prop=> prop.Place.PlaceID.StartsWith("W:") && !dc.Commands.Any(p=>p.Status < 3 && p.Source==prop.Place.PlaceID))
                           .OrderBy(prop => prop.TU.ProdDate)
                           .Take(dtoOrderGroup.Count())
                           .ToList()
                    })
                    .ToList();

                foreach (var r in res)
                {
                    if (r.DTOOrders.Count != r.Place.Count)
                        throw new Exception($"Warehouse does not have enough : {r.Key.SKU_ID}, {r.Key.SKU_Batch} x {r.Key.SKU_Qty}");
                    for (int i = 0; i < r.DTOOrders.Count; i++)
                    {
                        yield return new DTOCommand
                        {
                            TU_ID = r.Place[i].TU.TU_ID,
                            Order_ID = r.DTOOrders[i].ID,
                            Source = r.Place[i].Place.PlaceID,
                            Target = r.DTOOrders[i].Destination,
                        };
                    }
                }
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
            using (var dc = new WMSContext())
            {
                string destination = "";
                int counter = 0;
                IEnumerable<string> targets = null;
                foreach (Order o in orders)
                {
                    if (o.Destination != destination)
                    {
                        targets =
                            dc.PlaceIds
                            .Where(prop => prop.ID.StartsWith(o.Destination.Substring(0, 7)))
                            .Select(prop => prop.ID)
                            .ToList();
                        destination = o.Destination;
                        counter = 0;
                    }
                    double defQty = dc.SKU_IDs.Find(o.SKU_ID).DefaultQty;
                    for (int i = 0; i < o.SKU_Qty / defQty; i++)
                    {
                        DTOOrder dtoOrder = new DTOOrder(o);
                        dtoOrder.Destination = targets.ElementAt(counter);
                        dtoOrder.SKU_Qty = defQty;
                        counter = (++counter) % targets.Count();
                        yield return dtoOrder;
                    }
                    if (Math.Floor(o.SKU_Qty / defQty) * defQty < o.SKU_Qty)
                    {
                        DTOOrder dtoOrder = new DTOOrder(o);
                        dtoOrder.Destination = targets.ElementAt(counter);
                        dtoOrder.SKU_Qty = defQty;
                        yield return dtoOrder;
                    }
                    o.Status = 1;
                }
            }
        }


        public static Command ToCommand(this DTOCommand cmd)
        {
            return new Command
            {
                Order_ID = cmd.Order_ID,
                Source = cmd.Source,
                TU_ID = cmd.TU_ID,
                Status = cmd.Status,
                Target = cmd.Target
            };
        }

        public static MFCS_Proxy.DTOCommand ToProxyDTOCommand(this DTOCommand cmd)
        {
            return new MFCS_Proxy.DTOCommand
            {
                ID = cmd.ID,
                Order_ID = cmd.Order_ID.Value,
                Source = cmd.Source,
                TU_ID = cmd.TU_ID,
                Status = cmd.Status,
                Target = cmd.Target                
            };
        }

        public static IEnumerable<MFCS_Proxy.DTOCommand> ToProxyDTOCommand(this IEnumerable<Command> cmds)
        {
            return cmds.Select(p => new MFCS_Proxy.DTOCommand
            {
                ID = p.ID,
                Order_ID = p.Order_ID.Value,
                Source = p.Source,
                Status = p.Status,
                Target = p.Target,
                Time = p.Time,
                TU_ID = p.TU_ID
            });
        }



        public static IEnumerable<DTOCommand> TakeNeighbour(this IEnumerable<DTOCommand> commands)
        {
            using (var dc = new WMSContext())
            {
                var commandsList = commands.ToList();
                List<string> sourceList = commandsList                                            
                                         .Select(prop => prop.Source.Substring(0,10)+":1")
                                         .ToList();

                var Places =
                    (from place in dc.Places
                    where sourceList.Any(prop => prop == place.PlaceID)
                    select place).ToList();

                for (int i = 0; i < commandsList.Count(); i++)
                    yield return new DTOCommand
                    {
                        Order_ID = commandsList[i].Order_ID,
                        Source = commandsList[i].Source.Substring(0, 10) + ":1",
                        TU_ID = Places[i].TU_ID,
                        Target = null
                    };
            }
        }

        /// <summary>
        /// Command move inside warehouse to brother or free place (nearest or random)
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public static IEnumerable<DTOCommand> MoveToBrotherOrFree(this IEnumerable<DTOCommand> commands)
        {
            using (var dc = new WMSContext())
            {
                // Lambda outer join is very strange. 
                // therefore normal structure is used

                List<string> forbidden = new List<string>();


                var brothers =
                    (
                    from cmd in commands.ToList()
                    join tu in dc.TUs on cmd.TU_ID equals tu.TU_ID 
                    select new { TU = tu, Command = cmd } into join1
                    group join1 by new { Reck = join1.Command.Source.Substring(0, 3), join1.TU.SKU_ID, join1.TU.Batch, join1.TU.Qty } into gr
                    select new
                    {
                        Key = gr.Key,
                        Count = gr.Count(),
                        Commands = gr.
                                    Select(prop => prop.Command)
                                    .ToList(),
                        Brother =
                                (dc.TUs
                                .Where(p => p.SKU_ID == gr.Key.SKU_ID && p.Batch == gr.Key.Batch && p.Qty == gr.Key.Qty)
                                .Select(p => p.FK_TU_ID.FK_Place.FirstOrDefault().PlaceID)
                                .Where(p => gr.Key.Reck == p.Substring(0, 3) || p.StartsWith("T"))
                                .Union(
                                    dc.Commands
                                    .Where(p1 => p1.Status < 3 && p1.Target.StartsWith("W") && (gr.Key.Reck == p1.Target.Substring(0, 3) || gr.Key.Reck.StartsWith("T")))
                                    .Select(p1 => p1.FK_TU_ID.FK_TU.FirstOrDefault())
                                    .Where(p1 => p1.SKU_ID == gr.Key.SKU_ID && p1.Batch == gr.Key.Batch && p1.Qty == gr.Key.Qty)
                                    .Select(p1 => p1.FK_TU_ID.FK_Place.FirstOrDefault().PlaceID)
                                )
                                .Where(p => p.EndsWith("2"))
                                .Where(p => !dc.Places.Any(prop => prop.PlaceID == p.Substring(0, 10) + ":1"))
                                .Where(p => !dc.Commands.Any(prop => prop.Target.Substring(0, 10) == p.Substring(0, 10)))
                                .Take(gr.Count())
                                .Select(p => p.Substring(0, 10) + ":1")
                                ).ToList()
                    })
                    .ToList();

                    
                var lookNearest = new List<DTOCommand>();
                foreach (var br in brothers)
                {
                    lookNearest.AddRange(br.Commands.GetRange(br.Brother.Count(), br.Commands.Count() - br.Brother.Count()));
                    for (int i = 0; i < br.Brother.Count; i++)
                    {
                        forbidden.Add(br.Brother[i]);
                        yield return new DTOCommand
                        {
                            Order_ID = br.Commands[i].Order_ID,
                            Source = br.Commands[i].Source,
                            TU_ID = br.Commands[i].TU_ID,
                            Target = br.Brother[i]
                        };
                    }
                    for (int i = br.Brother.Count; i < br.Commands.Count; i++)
                    {
                        string target = br.Commands[i].GetRandomPlace(forbidden);
                        forbidden.Add(target);
                        yield return new DTOCommand
                        {
                            Order_ID = br.Commands[i].Order_ID,
                            Source = br.Commands[i].Source,
                            TU_ID = br.Commands[i].TU_ID,
                            Target = target
                        };
                    }
                }

                // empty pallets to be removed
                foreach (var cmd in commands)
                    if (dc.TUs.FirstOrDefault(prop => prop.TU_ID == cmd.TU_ID) == null)
                    {
                        string target = cmd.GetRandomPlace(forbidden);
                        forbidden.Add(target);
                        yield return new DTOCommand
                        {
                            Order_ID = cmd.Order_ID,
                            Source = cmd.Source,
                            TU_ID = cmd.TU_ID,
                            Target = target
                        };
                    }
            }
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
                            .FirstOrDefault(prop => prop.ID == command.TU_ID);
                var source = dc.PlaceIds.FirstOrDefault(prop => prop.ID == command.Source);

                var boothFree =
                    dc.PlaceIds
                    .Where(p => !p.FK_Places.Any()
                                && p.ID.EndsWith("2")
                                && p.DimensionClass == type.DimensionClass
                                && (command.Source.StartsWith("T") ||
                                    (p.ID.Substring(0, 3) == command.Source.Substring(0, 3)))
                                && !p.FK_Target_Commands.Any(prop => prop.Status < 3)
                                && !forbidden.Any(prop=> p.ID == prop))
                    .Join(dc.PlaceIds,
                            place => place.ID.Substring(0, 10) + ":1",
                            neighbour => neighbour.ID,
                            (place, neighbour) => new { Place = place, Neighbour = neighbour })
                    .Where(p => !p.Neighbour.FK_Places.Any()
                                    && !p.Neighbour.FK_Target_Commands.Any())
                    .Select(p => p.Place)
                    .OrderBy(prop=>prop.ID);

                if (command.Source.StartsWith("W"))
                    boothFree = boothFree
                                .OrderBy(prop => (prop.PositionHoist - source.PositionHoist) * (prop.PositionHoist - source.PositionHoist) +
                                                 (prop.PositionTravel - source.PositionTravel) * (prop.PositionTravel - source.PositionTravel));

                int count = 0;
                {
                    count = boothFree.Count();
                    if (count != 0)
                        return command.Source.StartsWith("W") ? boothFree.FirstOrDefault().ID : boothFree.Skip(Random.Next(count - 1)).FirstOrDefault().ID;

                    var oneFree =
                        dc.PlaceIds
                        .Where(p => !p.FK_Places.Any()
                                    && p.ID.EndsWith("1")
                                    && p.DimensionClass == type.DimensionClass
                                    && (command.Source.StartsWith("T") ||
                                        (p.ID.Substring(0, 3) == command.Source.Substring(0, 3)))
                                    && !p.FK_Target_Commands.Any(prop => prop.Status < 3)
                                    && !forbidden.Any(prop => p.ID == prop))
                       .OrderBy(prop=>prop.ID);

                    if (command.Source.StartsWith("W"))
                        oneFree = oneFree
                                    .OrderBy(prop => (prop.PositionHoist - source.PositionHoist) * (prop.PositionHoist - source.PositionHoist) +
                                                        (prop.PositionTravel - source.PositionTravel) * (prop.PositionTravel - source.PositionTravel));

                    count = oneFree.Count();
                    if (count != 0)
                        return command.Source.StartsWith("W") ? oneFree.FirstOrDefault().ID : oneFree.Skip(Random.Next(count - 1)).FirstOrDefault().ID;

                    throw new Exception($"Warehouse is full (demand from {command.Source})");
                }
            }
        }
    }
}


