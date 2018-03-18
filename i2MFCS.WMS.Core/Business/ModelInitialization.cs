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
    public class ModelInitialization
    {

        public void FillPlaceIDAndParams()
        {
            try
            {
                Task.WaitAll(FillPlaceIDs(), FillParameters());
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(ModelInitialization));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }


        private async Task FillParameters()
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    await dc.Database.ExecuteSqlCommandAsync($"DELETE FROM dbo.Parameters");

                    IEnumerable<Parameter> list = new List<Parameter>
                    {
                        new Parameter {Name="Order.CurrentERPID", Value="1"},
                        new Parameter {Name="Order.CurrentOrderID", Value="1"},
                        new Parameter {Name="Order.CurrentSubOrderID", Value="1"},
                        new Parameter {Name="InputCommand.Place", Value="T014"}
                    };
                    dc.Parameters.AddRange(list);
                    await dc.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(ModelInitialization));
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
                                select new PlaceID { ID = $"W:{rack:d2}:{travel:d3}:{hoist:d1}:{depth:d1}", PositionHoist = hoist, PositionTravel = travel };

                    var linq2 = from str in ConveyorNames()
                                select new PlaceID { ID = str };

                    var linq3 = (from truck in Enumerable.Range(1, 5)
                                 from row in Enumerable.Range(1, 4)
                                 select new PlaceID { ID = $"W:32:0{truck:d1}{row:d1}:1:1" });

                    var linq4 = new List<PlaceID>
                        {
                            new PlaceID { ID = "W:32:01", DimensionClass = -1},
                            new PlaceID { ID = "W:32:02", DimensionClass = -1},
                            new PlaceID { ID = "W:32:03", DimensionClass = -1},
                            new PlaceID { ID = "W:32:04", DimensionClass = -1},
                            new PlaceID { ID = "W:32:05", DimensionClass = -1},
                            new PlaceID { ID = "T04"}
                        };

                    dc.PlaceIds.AddRange(linq2.Union(linq1).Union(linq3).Union(linq4));
                    await dc.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(ModelInitialization));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        public void UpdateRackFrequencyClass(double[] abcPortions)
        {
            try
            {
                Task.WaitAll(UpdateRackFrequencyClassAsync(abcPortions));
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(ModelInitialization));
                Debug.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task UpdateRackFrequencyClassAsync(double[] abc)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    var query = dc.PlaceIds.Where(p => p.PositionTravel > 0 && p.PositionHoist > 0).OrderBy(pp => pp.PositionHoist * pp.PositionHoist + pp.PositionTravel * pp.PositionTravel);

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
                        slot.FrequencyClass = idx + 1;
                    }
                    await dc.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.AddException(ex, nameof(ModelInitialization));
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

    }
}
