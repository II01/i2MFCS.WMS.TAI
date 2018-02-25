using i2MFCS.WMS.Database.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Interface
{
    public class DbInterface 
    {
        public void CreateDatabase()
        {
            using (WMSContext dc = new WMSContext())
            {
                dc.Database.Delete();
                dc.Database.Create();
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

        private async Task FillPlaceIDs()
        {
            using (var dc = new WMSContext())
            {
                await dc.Database.ExecuteSqlCommandAsync($"DELETE FROM dbo.PlaceIDs");

                var linq1 = from rack in new List<int> { 11, 12, 21, 22}
                         from travel in Enumerable.Range(1, 126)
                         from hoist in Enumerable.Range(1, 9)
                         from depth in Enumerable.Range(1, 2)
                         select new PlaceID { ID = $"W:{rack:d2}:{travel:d3}:{hoist:d1}:{depth:d1}" } ;

                var linq2 = from str in ConveyorNames()
                            select new PlaceID { ID = str };

                var linq3 = (from truck in Enumerable.Range(1, 5)
                           from row in Enumerable.Range(1, 4)
                           select new PlaceID { ID = $"W:32:0:{truck:d1}:{row:d1}:1:1" });


                dc.PlaceIds.AddRange(linq2.Union(linq1).Union(linq3));
                await dc.SaveChangesAsync();
            }
        }

        public void FillPlaceID()
        {
            Task.WaitAll(FillPlaceIDs());
        }

        public void CreateInputCommands(int barcode)
        {
            using (var dc = new WMSContext())
            {

            }
        }

    }
}
