﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class Package_ID
    {
        [Key,MaxLength(30)]
        public string ID { get; set; }
        [ForeignKey("FK_SKU_ID"), MaxLength(30),Required,Index]
        public string SKU_ID { get; set; }
        [MaxLength(50),Required,Index]
        public string Batch { get; set; }
        public virtual SKU_ID FK_SKU_ID { get; set; }

        [InverseProperty("FK_Package_ID")]
        public virtual List<TU> FK_TU { get; set; }
        [InverseProperty("FK_Package_ID")]
        public virtual List<Order> FK_Order { get; set; }

        public override string ToString()
        {
            return $"({ID} = {SKU_ID} + {Batch})";
        }


    }
}
