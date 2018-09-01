﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class TU
    {
        [Key, Column(Order=0), ForeignKey("FK_TU_ID")]
        public int TU_ID { get; set; }
        [Key, Column(Order=1), ForeignKey("FK_Box_ID"), MaxLength(30)]
        public string Box_ID { get; set; }
        [Required,Index]
        public double Qty { get; set; }
        [Required,Index]
        public DateTime ProdDate { get; set; }
        [Required]
        public DateTime ExpDate { get; set; }

        public virtual TU_ID FK_TU_ID { get; set; }
        public virtual Box_ID FK_Box_ID { get; set; }
        public override string ToString()
        {
            return $"({TU_ID:d9}: {Box_ID}x{Qty})";
        }
    }
}
