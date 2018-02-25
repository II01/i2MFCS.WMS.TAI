using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class WMSContext : DbContext
    {
        public DbSet<Command> Commands { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<SKU_ID> SKU_IDs { get; set; }
        public DbSet<TU_ID> TU_IDs { get; set; }
        public DbSet<TU> TUs { get; set; }
        public DbSet<PlaceID> PlaceIds { get; set; }
        public DbSet<Place> Places { get; set; }
    }
}
