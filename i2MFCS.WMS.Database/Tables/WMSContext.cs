using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace i2MFCS.WMS.Database.Tables
{
    public class WMSContext : DbContext, IDisposable
    {
        public DbSet<PlaceID> PlaceIds { get; set; }
        public DbSet<TU_ID> TU_IDs { get; set; }
        public DbSet<SKU_ID> SKU_IDs { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<TU> TUs { get; set; }
        public DbSet<Command> Commands { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CommandERP> CommandERP { get; set; }
        public DbSet<Parameter> Parameters { get; set; } 
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            /*
             * If Required is set for Target and Source we get exception on WillCascadeDelete
             * 
             *
             */
            modelBuilder.Entity<Command>()
                .HasRequired<PlaceID>(c => c.FK_Target )
                .WithMany( c => c.FK_Target_Commands)
                .HasForeignKey( c => c.Target)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Command>()
                .HasRequired<Order>(c => c.FK_OrderID)
                .WithMany(c => c.FK_Commands)
                .HasForeignKey(c => c.Order_ID)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);

        }
    }
}
