using i2MFCS.WMS.Database.Tables;
using SimpleLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        public void FillPlaceID()
        {
            try
            {
                Task.WaitAll(FillPlaceIDs());
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        // strict FIFO 
        public void CreateOutputCommands(int erpID, int subOrderID, string targetQuery)
        {
            try
            {
                int output = 0;
                List<Command> cmdList = new List<Command>();
                using (var dc = new WMSContext())
                {
                    List<string> target = dc.PlaceIds
                                          .Where(prop => prop.ID.StartsWith(targetQuery))
                                          .Select( prop => prop.ID)
                                          .ToList();
                    foreach (Order o in dc.Orders.Where((o) => o.Status == 0 && o.ERP_ID == erpID && o.SubOrderID == subOrderID))
                    {
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

                    var cmdSorted = from cmd in cmdList
                                    orderby cmd.Source.EndsWith("1") descending, cmd.Source
                                    select cmd;

                    foreach (Command cmd in cmdSorted)
                    {
                        // Make internal warehouse transfer if neceseary
                        if (cmd.Source.EndsWith("2") && dc.Places.FirstOrDefault( prop => prop.PlaceID == cmd.Source.Substring(0,10)+":1") != null &&
                            cmdSorted.FirstOrDefault( prop => prop.Source == cmd.Source.Substring(0, 10) + ":1") == null)
                        {
                            string brother = FindBrotherInside(dc, cmd.TU_ID, cmdSorted.Select(prop => prop.Source));
                            if (brother == null)
                            {
                                PlaceID nearest = dc.PlaceIds.Find(cmd.Source);
                                brother = FindFreeNearestWindow(dc, dc.TU_IDs.Find(cmd.TU_ID).DimensionClass, cmdSorted.Select(prop => prop.Source),  nearest);
                                if (brother == null)
                                    throw new Exception("Warehouse is full");
                                Command transferCmd = new Command
                                {
                                    Order_ID = cmd.Order_ID,
                                    Source = cmd.Source.Substring(0, 10) + ":1",
                                    Target = brother,
                                    Status = 0
                                };
                                dc.Commands.Add(transferCmd);
                                Log.AddLog(Log.Severity.EVENT, nameof(Model), $"Transfer command created ({transferCmd.ToString()})","");
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
                    // dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void CreateInputCommand(string source)
        {
            try
            {
                using (var dc = new WMSContext())
                {
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
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void UpdateRackFrequencyClass(double [] abcPortions)
        {
            try
            {
                Task.WaitAll(UpdateRackFrequencyClassAsync(abcPortions));
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
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
                Log.AddException(ex, nameof(Model));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


    }
}
