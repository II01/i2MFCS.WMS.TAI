namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Hist : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Orders", "Destination", "dbo.PlaceIDs");
            DropForeignKey("dbo.Places", "PlaceID", "dbo.PlaceIDs");
            DropForeignKey("dbo.Commands", "Source", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistOrders", "Destination", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistCommands", "Source", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistCommands", "Target", "dbo.PlaceIDs");
            DropForeignKey("dbo.Commands", "Target", "dbo.PlaceIDs");
            DropIndex("dbo.Orders", new[] { "Destination" });
            DropIndex("dbo.Commands", new[] { "Source" });
            DropIndex("dbo.Commands", new[] { "Target" });
            DropIndex("dbo.Places", new[] { "PlaceID" });
            DropIndex("dbo.HistCommands", new[] { "Source" });
            DropIndex("dbo.HistCommands", new[] { "Target" });
            DropIndex("dbo.HistOrders", new[] { "Destination" });
            DropPrimaryKey("dbo.PlaceIDs");
            DropPrimaryKey("dbo.Places");
            AddColumn("dbo.HistOrders", "TU_ID", c => c.Int(nullable: false));
            AddColumn("dbo.HistOrders", "SKU_Package", c => c.String(nullable: false, maxLength: 30));
            AddColumn("dbo.HistOrders", "Type", c => c.Int(nullable: false));
            AddColumn("dbo.HistOrders", "FK_Package_ID_ID", c => c.String(maxLength: 30));
            AlterColumn("dbo.Orders", "Destination", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.Commands", "Source", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.Commands", "Target", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.PlaceIDs", "ID", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.Places", "PlaceID", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.HistCommands", "Source", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.HistCommands", "Target", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.HistOrders", "Destination", c => c.String(nullable: false, maxLength: 15));
            AddPrimaryKey("dbo.PlaceIDs", "ID");
            AddPrimaryKey("dbo.Places", new[] { "PlaceID", "TU_ID" });
            CreateIndex("dbo.Orders", "Destination");
            CreateIndex("dbo.Commands", "Source");
            CreateIndex("dbo.Commands", "Target");
            CreateIndex("dbo.HistOrders", "TU_ID");
            CreateIndex("dbo.HistOrders", "Destination");
            CreateIndex("dbo.HistOrders", "FK_Package_ID_ID");
            CreateIndex("dbo.HistCommands", "Source");
            CreateIndex("dbo.HistCommands", "Target");
            CreateIndex("dbo.Places", "PlaceID");
            AddForeignKey("dbo.HistOrders", "FK_Package_ID_ID", "dbo.Package_ID", "ID");
            AddForeignKey("dbo.HistOrders", "TU_ID", "dbo.TU_ID", "ID", cascadeDelete: true);
            AddForeignKey("dbo.HistCommands", "Target", "dbo.PlaceIDs", "ID");
            AddForeignKey("dbo.HistOrders", "Destination", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.Orders", "Destination", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.Places", "PlaceID", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.Commands", "Source", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.HistCommands", "Source", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.Commands", "Target", "dbo.PlaceIDs", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Commands", "Target", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistCommands", "Source", "dbo.PlaceIDs");
            DropForeignKey("dbo.Commands", "Source", "dbo.PlaceIDs");
            DropForeignKey("dbo.Places", "PlaceID", "dbo.PlaceIDs");
            DropForeignKey("dbo.Orders", "Destination", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistOrders", "Destination", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistCommands", "Target", "dbo.PlaceIDs");
            DropForeignKey("dbo.HistOrders", "TU_ID", "dbo.TU_ID");
            DropForeignKey("dbo.HistOrders", "FK_Package_ID_ID", "dbo.Package_ID");
            DropIndex("dbo.Places", new[] { "PlaceID" });
            DropIndex("dbo.HistCommands", new[] { "Target" });
            DropIndex("dbo.HistCommands", new[] { "Source" });
            DropIndex("dbo.HistOrders", new[] { "FK_Package_ID_ID" });
            DropIndex("dbo.HistOrders", new[] { "Destination" });
            DropIndex("dbo.HistOrders", new[] { "TU_ID" });
            DropIndex("dbo.Commands", new[] { "Target" });
            DropIndex("dbo.Commands", new[] { "Source" });
            DropIndex("dbo.Orders", new[] { "Destination" });
            DropPrimaryKey("dbo.Places");
            DropPrimaryKey("dbo.PlaceIDs");
            AlterColumn("dbo.HistOrders", "Destination", c => c.String(nullable: false, maxLength: 30));
            AlterColumn("dbo.HistCommands", "Target", c => c.String(nullable: false, maxLength: 30));
            AlterColumn("dbo.HistCommands", "Source", c => c.String(nullable: false, maxLength: 30));
            AlterColumn("dbo.Places", "PlaceID", c => c.String(nullable: false, maxLength: 30));
            AlterColumn("dbo.PlaceIDs", "ID", c => c.String(nullable: false, maxLength: 30));
            AlterColumn("dbo.Commands", "Target", c => c.String(nullable: false, maxLength: 30));
            AlterColumn("dbo.Commands", "Source", c => c.String(nullable: false, maxLength: 30));
            AlterColumn("dbo.Orders", "Destination", c => c.String(nullable: false, maxLength: 30));
            DropColumn("dbo.HistOrders", "FK_Package_ID_ID");
            DropColumn("dbo.HistOrders", "Type");
            DropColumn("dbo.HistOrders", "SKU_Package");
            DropColumn("dbo.HistOrders", "TU_ID");
            AddPrimaryKey("dbo.Places", new[] { "PlaceID", "TU_ID" });
            AddPrimaryKey("dbo.PlaceIDs", "ID");
            CreateIndex("dbo.HistOrders", "Destination");
            CreateIndex("dbo.HistCommands", "Target");
            CreateIndex("dbo.HistCommands", "Source");
            CreateIndex("dbo.Places", "PlaceID");
            CreateIndex("dbo.Commands", "Target");
            CreateIndex("dbo.Commands", "Source");
            CreateIndex("dbo.Orders", "Destination");
            AddForeignKey("dbo.Commands", "Target", "dbo.PlaceIDs", "ID");
            AddForeignKey("dbo.HistCommands", "Target", "dbo.PlaceIDs", "ID");
            AddForeignKey("dbo.HistCommands", "Source", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.HistOrders", "Destination", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.Commands", "Source", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.Places", "PlaceID", "dbo.PlaceIDs", "ID", cascadeDelete: true);
            AddForeignKey("dbo.Orders", "Destination", "dbo.PlaceIDs", "ID", cascadeDelete: true);
        }
    }
}
