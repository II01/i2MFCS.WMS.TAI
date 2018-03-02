using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Interface
{


    public static class ModelExtensions
    {
        public static IEnumerable<Command> ToCommands(this IEnumerable<Place> list, int orderID, string target)
        {
            foreach (Place p in list)
                yield return new Command { TU_ID = p.TU_ID, Source = p.PlaceID, Target = target, Order_ID = orderID, Status = 0 };
        }
    }

    public class Model
    {
        private static Random Random = new Random();

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
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        public void FillPlaceID()
        {
            Task.WaitAll(FillPlaceIDs());
        }


        // strict FIFO 
        public void CreateOutputCommands(int erpID, string batch, List<string> target)
        {
            try
            {
                int output = 0;
                List<Command> cmdList = new List<Command>();
                using (var dc = new WMSContext())
                {
                    foreach (Order o in dc.Orders.Where((o) => o.Status == 0 && o.ERP_ID == erpID && o.SKU_Batch == batch))
                    {
                        double defQty = dc.SKU_IDs.Find(o.SKU_ID).DefaultQty;
                        int count = Convert.ToInt32(o.SKU_Qty / defQty);
                        if (count > 0)
                        {
                            cmdList.AddRange(FIFO_FindSKUWithQty(dc, count, o.SKU_ID, defQty).ToCommands(o.ID, ""));
                            if (cmdList.Count() != count)
                                throw new Exception($"Warehouse does not have enough SKU_ID = {o.SKU_ID}");
                        }
                        if (o.SKU_Qty - count * defQty > 0)
                            cmdList.Add(FIFO_FindSKUWithQty(dc, 1, o.SKU_ID, o.SKU_Qty - count * defQty).ToCommands(o.ID, "").First());

                        o.Status = 1;
                    }

                    var cmdSorted = from cmd in cmdList
                                    orderby cmd.Source.EndsWith("1") descending, cmd.Source
                                    select cmd;

                    foreach (Command cmd in cmdSorted)
                    {
                        // Make internal warehouse transfer if neceseary
                        if (cmd.Source.EndsWith("2") && dc.Places.FirstOrDefault( prop => prop.PlaceID == cmd.Source.Substring(0,10)+":1") != null &&
                            cmdSorted.FirstOrDefault( prop => prop.Source == cmd.Source.Substring(0, 10) + ":1") == null)
                        {
                            string brother = FindBrotherInside(dc, cmd.TU_ID, cmdSorted.Select(prop => prop.Source).AsEnumerable<string>());
                            if (brother == null)
                            {
                                var freeP = FindFreePlaces(dc, dc.TU_IDs.First(prop => prop.ID == cmd.TU_ID).DimensionClass, null);
                                brother = (from p in freeP
                                            orderby Math.Abs(SqlFunctions.IsNumeric(p.Key.Substring(2,2)).Value - SqlFunctions.IsNumeric(cmd.Source.Substring(2,2)).Value),
                                                    Math.Abs(SqlFunctions.IsNumeric(p.Key.Substring(5,3)).Value - SqlFunctions.IsNumeric(cmd.Source.Substring(5,3)).Value +
                                                            SqlFunctions.IsNumeric(p.Key.Substring(9,1)).Value - SqlFunctions.IsNumeric(cmd.Source.Substring(9,1)).Value) 
                                            select p.Key).First() + ":2";
                                Command transferCmd = new Command
                                {
                                    Order_ID = cmd.Order_ID,
                                    Source = cmd.Source.Substring(0, 10) + ":1",
                                    Target = brother,
                                    Status = 0
                                };
                                Debug.WriteLine($"Command.Add({transferCmd.ToString()})");
                                dc.Commands.Add(transferCmd);
                            }

                            // make inside warehouse transfer
                        }

                        // Assign targets to
                        cmd.Target = target[output];
                        output = (output + 1) % 4;
                        dc.Commands.Add(cmd);
                        Debug.WriteLine($"Command.Add({cmd.ToString()})");
                    }
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void CreateInputCommands(string source, int barcode, int size)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    Command cmd = null;
                    string brother = FindBrotherInside(dc, barcode, null);
                    if (brother != null)
                    {
                        cmd = new Command
                        {
                            Order_ID = null,
                            TU_ID = barcode,
                            Source = source,
                            Target = brother,
                            Status = 0
                        };
                    }
                    else
                    {
                        var freeP = FindFreePlaces(dc, size, null).ToList();
                        if (freeP.Count() == 0)
                            throw new Exception("Warehouse is full");
                        PlaceID tar = freeP[Random.Next(freeP.Count() - 1)].Last();
                        cmd = new Command
                        {
                            Order_ID = null,
                            TU_ID = barcode,
                            Source = source,
                            Target = tar.ID,
                            Status = 0
                        };
                    }

                    if (cmd != null && dc.Commands.FirstOrDefault((prop) => prop.Status < 3 && prop.TU_ID == barcode) == null)
                    {
                        dc.Commands.Add(cmd);
                        Debug.WriteLine($"Commands.Add({cmd.ToString()})");
                        dc.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        // TODO - brother should also look for active commands brother (on the way to warehouse)
        private string FindBrotherInside(WMSContext dc, int barcode, IEnumerable<string> forbidden)
        {
            try
            {
                TU p = dc.TUs.First(prop => prop.TU_ID == barcode);
                string skuid = p.SKU_ID;                
                int qty = p.Qty;

                List<string> inputWh = new List<string> { "W:11", "W:12", "W:21", "W:22" };

                string found = 
                        (from tu in dc.TUs
                        join place in dc.Places on tu.TU_ID equals  place.TU_ID
                        join neighbor in dc.Places on place.PlaceID.Substring(0,10)+":1" equals neighbor.PlaceID into neigborJoin
                        from neighborOrNull in neigborJoin.DefaultIfEmpty()
                        where   tu.SKU_ID == skuid && tu.Qty == qty &&
                                place.PlaceID.EndsWith("2") &&
                                inputWh.Any(p1 => place.PlaceID.StartsWith(p1)) &&
                                neighborOrNull == null &&
                                !(from cmd in dc.Commands
                                  where cmd.Status < 3 &&
                                        cmd.Source == place.PlaceID
                                  select cmd).Any() &&
                                !(from cmd in dc.Commands
                                  where cmd.Status < 3 &&
                                        cmd.Target.Substring(0,10) == place.PlaceID.Substring(0,10)
                                  select cmd).Any() &&
                                  (forbidden == null || forbidden.Any( (prop) => place.PlaceID.StartsWith(prop)))
                        orderby place.TimeStamp descending
                        select place.PlaceID).FirstOrDefault();
                if (found != null)
                    return found.Substring(0, 10) + ":1";
                else
                    return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private IEnumerable<IGrouping<string, PlaceID>> FindFreePlaces(WMSContext dc, int dimensionclass, IEnumerable<string> forbidden)
        {
            try
            {
                List<string> inputWh = new List<string> { "W:11", "W:12", "W:21", "W:22" };

                return 
                        (from placeID in dc.PlaceIds
                        where 
                            placeID.FK_Places.Count() == 0 &&
                            placeID.DimensionClass == dimensionclass &&
                            inputWh.Any(p1 => placeID.ID.StartsWith(p1)) &&
                            !(from cmd in dc.Commands
                              where cmd.Status < 3 &&
                              cmd.Target.Substring(0,10) == placeID.ID.Substring(0,10)
                              select cmd).Any() &&
                            (forbidden == null || forbidden.Any((prop) => placeID.ID.StartsWith(prop)))
                        group placeID by placeID.ID.Substring(0, 10) into g
                        select g);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }

        }


        private IEnumerable<string> ConveyorNames()
        {
            yield return "C101";
            yield return "C102";
            yield return "C201";
            yield return "C202";
            yield return "C301";
            yield return "T013";
            yield return "T014";
            yield return "T015";
            yield return "T021";
            yield return "T022";
            yield return "T023";
            yield return "T024";
            yield return "T025";
            yield return "T031";
            yield return "T032";
            yield return "T033";
            yield return "T034";
            yield return "T035";
            yield return "T036";
            yield return "T037";
            yield return "T038";
            yield return "T111";
            yield return "T112";
            yield return "T113";
            yield return "T114";
            yield return "T115";
            yield return "T121";
            yield return "T122";
            yield return "T123";
            yield return "T124";
            yield return "T125";
            yield return "T211";
            yield return "T212";
            yield return "T213";
            yield return "T214";
            yield return "T215";
            yield return "T221";
            yield return "T222";
            yield return "T223";
            yield return "T224";
            yield return "T225";
            yield return "T041";
            yield return "T042";
        }

        private IEnumerable<Place> FIFO_FindSKUWithQty(WMSContext dc, int count, string skuid, double dem_qty)
        {
            try
            {
                return (
                            from p in dc.Places
                            join tu in dc.TUs on p.TU_ID equals tu.TU_ID
                            where tu.SKU_ID == skuid &&          // paletts with correct SKU
                                   tu.Qty == dem_qty &&           // only demanded quantity
                                   !(from cmd in dc.Commands      // check if another command is not already active  
                                    where   cmd.Source == p.PlaceID &&
                                            cmd.Status < 3
                                    select cmd).Any() &&
                                   !(from cmd in dc.Commands
                                     where  cmd.Target.Substring(0,10) == p.PlaceID.Substring(0,10) &&
                                            cmd.Status < 3
                                     select cmd).Any()
                            orderby p.TimeStamp descending          // strict FIFO 
                            select p)
                           .Take(count);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task FillPlaceIDs()
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    await dc.Database.ExecuteSqlCommandAsync($"DELETE FROM dbo.PlaceIDs");

                    var linq1 = from rack in new List<int> { 11, 12, 21, 22 }
                                from travel in Enumerable.Range(1, 126)
                                from hoist in Enumerable.Range(1, 9)
                                from depth in Enumerable.Range(1, 2)
                                select new PlaceID { ID = $"W:{rack:d2}:{travel:d3}:{hoist:d1}:{depth:d1}", PositionHoist = hoist , PositionTravel = travel};

                    var linq2 = from str in ConveyorNames()
                                select new PlaceID { ID = str };

                    var linq3 = (from truck in Enumerable.Range(1, 5)
                                 from row in Enumerable.Range(1, 4)
                                 select new PlaceID { ID = $"W:32:0{truck:d1}{row:d1}:1:1" });


                    dc.PlaceIds.AddRange(linq2.Union(linq1).Union(linq3));
                    await dc.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void UpdateRackFrequencyClass(double [] abcPortions)
        {
            Task.WaitAll(UpdateRackFrequencyClassAsync(abcPortions));
        }

        private async Task UpdateRackFrequencyClassAsync(double [] abc)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    var query = dc.PlaceIds.Where(p => p.PositionTravel > 0 && p.PositionHoist > 0).OrderBy(pp => pp.PositionHoist*pp.PositionHoist + pp.PositionTravel*pp.PositionTravel);

                    int m = query.Count();
                    int count = 0;
                    int idx = 0;
                    int idxmax = 0;
                    double range = 0;
                    if (abc == null || abc.Length == 0)
                    {
                        idxmax = 0;
                        range = 1.0;
                    }
                    else
                    {
                        idxmax = abc.Length;
                        range = abc[0];
                    }
                    foreach (var slot in query)
                    {
                        count++;
                        if (count / (double)m > range)
                        {
                            idx++;
                            if (idx < idxmax)
                                range += abc[idx];
                            else
                                range = 1.0;
                        }
                        slot.FrequencyClass = idx+1;
                    }
                    await dc.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


    }
}
