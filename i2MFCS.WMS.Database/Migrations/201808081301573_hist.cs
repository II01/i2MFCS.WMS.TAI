namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class hist : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HistCommands",
                c => new
                    {
                        ID = c.Int(nullable: false),
                        Order_ID = c.Int(nullable: false),
                        TU_ID = c.Int(nullable: false),
                        Source = c.String(nullable: false, maxLength: 30),
                        Target = c.String(nullable: false, maxLength: 30),
                        Status = c.Int(nullable: false),
                        Time = c.DateTime(nullable: false),
                        LastChange = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.HistOrders", t => t.Order_ID)
                .ForeignKey("dbo.PlaceIDs", t => t.Source, cascadeDelete: true)
                .ForeignKey("dbo.PlaceIDs", t => t.Target)
                .ForeignKey("dbo.TU_ID", t => t.TU_ID, cascadeDelete: true)
                .Index(t => t.Order_ID)
                .Index(t => t.TU_ID)
                .Index(t => t.Source)
                .Index(t => t.Target);
            
            CreateTable(
                "dbo.HistOrders",
                c => new
                    {
                        ID = c.Int(nullable: false),
                        ERP_ID = c.Int(),
                        OrderID = c.Int(nullable: false),
                        SubOrderID = c.Int(nullable: false),
                        SubOrderERPID = c.Int(nullable: false),
                        SKU_ID = c.String(nullable: false, maxLength: 30),
                        SubOrderName = c.String(nullable: false, maxLength: 200),
                        SKU_Qty = c.Double(nullable: false),
                        Destination = c.String(nullable: false, maxLength: 30),
                        ReleaseTime = c.DateTime(nullable: false),
                        SKU_Batch = c.String(nullable: false, maxLength: 50),
                        Status = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.CommandERPs", t => t.ERP_ID)
                .ForeignKey("dbo.PlaceIDs", t => t.Destination, cascadeDelete: true)
                .ForeignKey("dbo.SKU_ID", t => t.SKU_ID, cascadeDelete: true)
                .Index(t => t.ERP_ID)
                .Index(t => t.SKU_ID)
                .Index(t => t.Destination);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.HistCommands", "TU_ID", "dbo.TU_ID");
            DropForeignKey("dbo.HistCommands", "Target", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistCommands", "Source", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistCommands", "Order_ID", "dbo.HistOrders");
            DropForeignKey("dbo.HistOrders", "SKU_ID", "dbo.SKU_ID");
            DropForeignKey("dbo.HistOrders", "Destination", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistOrders", "ERP_ID", "dbo.CommandERPs");
            DropIndex("dbo.HistOrders", new[] { "Destination" });
            DropIndex("dbo.HistOrders", new[] { "SKU_ID" });
            DropIndex("dbo.HistOrders", new[] { "ERP_ID" });
            DropIndex("dbo.HistCommands", new[] { "Target" });
            DropIndex("dbo.HistCommands", new[] { "Source" });
            DropIndex("dbo.HistCommands", new[] { "TU_ID" });
            DropIndex("dbo.HistCommands", new[] { "Order_ID" });
            DropTable("dbo.HistOrders");
            DropTable("dbo.HistCommands");
        }
    }
}
