namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test1 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs");
            DropIndex("dbo.Orders", new[] { "ERP_ID" });
            AlterColumn("dbo.Orders", "ERP_ID", c => c.Int());
            CreateIndex("dbo.Orders", "ERP_ID");
            AddForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs");
            DropIndex("dbo.Orders", new[] { "ERP_ID" });
            AlterColumn("dbo.Orders", "ERP_ID", c => c.Int(nullable: false));
            CreateIndex("dbo.Orders", "ERP_ID");
            AddForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs", "ID", cascadeDelete: true);
        }
    }
}
