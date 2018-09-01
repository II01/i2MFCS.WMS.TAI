using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class Command
    {
        public enum CommandStatus { NotActive = 0, Active, Canceled, Finished}
        public enum CommandOperation { None = 0, Move, StoreTray, RetrieveTray, DropBox, PickBox, Confirm }
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [ForeignKey("FK_OrderID")]
        public int? Order_ID { get; set; }
        [Required,ForeignKey("FK_TU_ID")]
        public int TU_ID { get; set; }
        [ForeignKey("FK_Box_ID")]
        public string Box_ID { get; set; }
        [Required,MaxLength(15), ForeignKey("FK_Source")]
        public string Source { get; set; }
        [Required,MaxLength(15), ForeignKey("FK_Target")]
        public string Target { get; set; }
        [Required]
        public CommandOperation Operation { get; set; }
        [Required]
        public CommandStatus Status { get; set; }
        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Time { get; set; }
        public DateTime LastChange { get; set; }
        public virtual TU_ID FK_TU_ID { get; set; }
        public virtual Box_ID FK_Box_ID { get; set; }
        public virtual PlaceID FK_Source { get; set; }
        public virtual PlaceID FK_Target { get; set; }
        public virtual Order FK_OrderID { get; set; }

        public override string ToString()
        {
            return $"({ID},{Order_ID??0}):{TU_ID:d9}:{Box_ID??""}{Source}->{Target}";
        }
    }
}
