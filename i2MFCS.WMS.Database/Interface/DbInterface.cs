using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Interface
{
    public class DbInterface
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
        public void CreateOutputCommands(int erpID, List<string> target)
        {
            try
            {
                int output = 0;
                List<Command> cmdList = new List<Command>();
                using (var dc = new WMSContext())
                {
                    foreach (Order o in dc.Orders.Where((o) => o.Status == 0 && o.ERP_ID == erpID))
                    {
                        double defQty = dc.SKU_IDs.Find(o.SKU_ID).DefaultQty;
                        int count = Convert.ToInt32(o.Qty / defQty);
                        if (count > 0)
                        {
                            cmdList.AddRange(FIFO_FindSKUWithQty(count, o.SKU_ID, defQty).ToList());
                            if (cmdList.Count() != count)
                                throw new Exception($"Warehouse does not have enough SKU_ID = {o.SKU_ID}");
                        }
                        cmdList.Add(FIFO_FindSKUWithQty(1, o.SKU_ID, o.Qty - count * defQty).First());
                        foreach (Command cmd in cmdList)
                        {
                            // TODO make inside warehouse movement 
                            cmd.Target = target[output];
                            output = (output + 1) % 4;
                            dc.Commands.Add(cmd);
                        }
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



        public void CreateInputCommands(string source, int barcode, int size)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    List<string> inputWh = new List<string> { "W:11", "W:12", "W:21", "W:22" };

                    var freeP = FindFreePlaces(size).ToList();

                    if (freeP.Count() == 0)
                        throw new Exception("Warehouse is full");

                    PlaceID tar = freeP[Random.Next(freeP.Count() - 1)].First();
                    dc.Commands.Add(new Command
                    {
                        TU_ID = barcode,
                        Source = source,
                        Target = tar.ID,
                        Status = 0
                    });
                    dc.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private IEnumerable<IGrouping<string, PlaceID>> FindFreePlaces(int size)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    List<string> inputWh = new List<string> { "W:11", "W:12", "W:21", "W:22" };

                    return (from placeID in dc.PlaceIds
                            where placeID.FK_Place.Count() == 0 &&
                            placeID.Size == size &&
                            inputWh.Any(p1 => placeID.ID.StartsWith(p1))
                            group placeID by placeID.ID.Substring(0, 10) into g
                            select g);
                }
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

        private IEnumerable<Command> FIFO_FindSKUWithQty(int count, string skuid, double dem_qty)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    return (from p in dc.Places
                            join tu in dc.TUs on p.TU_ID equals tu.TU_ID
                            where tu.SKU_ID == skuid &&          // paletts with correct SKU
                                  tu.Qty == dem_qty &&           // only demanded quantity
                                  !(from cmd in dc.Commands      // check if another command is not already active  
                                    where cmd.Source == p.PlaceID &&
                                    cmd.Status < 3
                                    select cmd).Any()
                            orderby p.TimeStamp descending          // strict FIFO 
                            select new Command
                            {
                                TU_ID = p.TU_ID,
                                Source = p.PlaceID,
                                Target = "",
                                Status = 0
                            })
                            .Take(count)
                            .AsEnumerable<Command>();
                }
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
                                select new PlaceID { ID = $"W:{rack:d2}:{travel:d3}:{hoist:d1}:{depth:d1}" };

                    var linq2 = from str in ConveyorNames()
                                select new PlaceID { ID = str };

                    var linq3 = (from truck in Enumerable.Range(1, 5)
                                 from row in Enumerable.Range(1, 4)
                                 select new PlaceID { ID = $"W:32:0:{truck:d1}:{row:d1}:1:1" });


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

    }
}
