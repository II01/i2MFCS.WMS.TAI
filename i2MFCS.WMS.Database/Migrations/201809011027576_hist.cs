namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class hist : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistOrders", "Box_ID", c => c.String(nullable: false, maxLength: 30));
            AddColumn("dbo.HistCommands", "Box_ID", c => c.String(maxLength: 30));
            AddColumn("dbo.HistCommands", "Type", c => c.Int(nullable: false));
            CreateIndex("dbo.HistCommands", "Box_ID");
            AddForeignKey("dbo.HistCommands", "Box_ID", "dbo.Box_ID", "ID");
            DropColumn("dbo.HistOrders", "Box");
        }
        
        public override void Down()
        {
            AddColumn("dbo.HistOrders", "Box", c => c.String(nullable: false, maxLength: 30));
            DropForeignKey("dbo.HistCommands", "Box_ID", "dbo.Box_ID");
            DropIndex("dbo.HistCommands", new[] { "Box_ID" });
            DropColumn("dbo.HistCommands", "Type");
            DropColumn("dbo.HistCommands", "Box_ID");
            DropColumn("dbo.HistOrders", "Box_ID");
        }
    }
}
