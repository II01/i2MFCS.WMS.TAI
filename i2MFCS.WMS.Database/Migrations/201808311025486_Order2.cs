namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Order2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "TU_ID", c => c.Int(nullable: false));
            CreateIndex("dbo.Orders", "TU_ID");
            AddForeignKey("dbo.Orders", "TU_ID", "dbo.TU_ID", "ID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orders", "TU_ID", "dbo.TU_ID");
            DropIndex("dbo.Orders", new[] { "TU_ID" });
            DropColumn("dbo.Orders", "TU_ID");
        }
    }
}
