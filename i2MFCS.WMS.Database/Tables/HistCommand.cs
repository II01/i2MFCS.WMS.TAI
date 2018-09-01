using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class HistCommand
    {
        public enum HistCommandStatus { NotActive = 0, Active, Canceled, Finished }
        public enum HistCommandType { StoreTray = 0, RetrieveTray, DropBox, PickBox }
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        [ForeignKey("FK_HistOrderID")]
        public int? Order_ID { get; set; }
        [Required, ForeignKey("FK_TU_ID")]
        public int TU_ID { get; set; }
        [ForeignKey("FK_Box_ID")]
        public string Box_ID { get; set; }
        [Required, MaxLength(15), ForeignKey("FK_Source")]
        public string Source { get; set; }
        [Required, MaxLength(15), ForeignKey("FK_Target")]
        public string Target { get; set; }
        [Required]
        public HistCommandType Type { get; set; }
        [Required]
        public HistCommandStatus Status { get; set; }
        [Required]
        public DateTime Time { get; set; }
        public DateTime LastChange { get; set; }
        public virtual TU_ID FK_TU_ID { get; set; }
        public virtual Box_ID FK_Box_ID { get; set; }
        public virtual PlaceID FK_Source { get; set; }
        public virtual PlaceID FK_Target { get; set; }
        public virtual HistOrder FK_HistOrderID { get; set; }

        public override string ToString()
        {
            return $"({ID},{Order_ID ?? 0}):{TU_ID:d9}:{Box_ID ?? ""}{Source}->{Target}";
        }
    }
}
