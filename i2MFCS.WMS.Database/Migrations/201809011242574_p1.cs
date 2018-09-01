namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class p1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "Operation", c => c.Int(nullable: false));
            AddColumn("dbo.HistOrders", "Operation", c => c.Int(nullable: false));
            DropColumn("dbo.Orders", "Type");
            DropColumn("dbo.HistOrders", "Type");
        }
        
        public override void Down()
        {
            AddColumn("dbo.HistOrders", "Type", c => c.Int(nullable: false));
            AddColumn("dbo.Orders", "Type", c => c.Int(nullable: false));
            DropColumn("dbo.HistOrders", "Operation");
            DropColumn("dbo.Orders", "Operation");
        }
    }
}
