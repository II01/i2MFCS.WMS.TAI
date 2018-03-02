namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test11 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CommandERPs",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CommandDecription = c.String(nullable: false),
                        Status = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID);
            
            AddColumn("dbo.Orders", "OrderID", c => c.Int(nullable: false));
            AddColumn("dbo.Orders", "SubOrderID", c => c.Int(nullable: false));
            CreateIndex("dbo.Orders", "ERP_ID");
            AddForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs", "ID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs");
            DropIndex("dbo.Orders", new[] { "ERP_ID" });
            DropColumn("dbo.Orders", "SubOrderID");
            DropColumn("dbo.Orders", "OrderID");
            DropTable("dbo.CommandERPs");
        }
    }
}
