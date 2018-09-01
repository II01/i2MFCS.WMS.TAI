﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{

    public class SKU_ID
    {
        [Key]
        [MaxLength(30)]
        public string ID { get; set; }
        [MaxLength(50)]
        public string Description { get; set; }
        [Required]
        public double DefaultQty { get; set; }
        [MaxLength(10), Required]
        public string Unit { get; set; }
        [Required]
        public double Weight { get; set; }
        [Required]
        public int FrequencyClass { get; set; }
        [Required]
        public int Length { get; set; }
        [Required]
        public int Width { get; set; }
        [Required]
        public int Height { get; set; }
        [InverseProperty("FK_SKU_ID")]
        public virtual List<Order> FK_Orders {get;set;}
        public override string ToString()
        {
            return $"({ID},{Description ?? ""},{DefaultQty},{Unit},{Weight})";
        }
    }
}
