namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Order : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "SKU_Package", c => c.String(nullable: false, maxLength: 30));
            AddColumn("dbo.Orders", "Type", c => c.Int(nullable: false));
            AddColumn("dbo.Orders", "FK_Package_ID_ID", c => c.String(maxLength: 30));
            CreateIndex("dbo.Orders", "FK_Package_ID_ID");
            AddForeignKey("dbo.Orders", "FK_Package_ID_ID", "dbo.Package_ID", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orders", "FK_Package_ID_ID", "dbo.Package_ID");
            DropIndex("dbo.Orders", new[] { "FK_Package_ID_ID" });
            DropColumn("dbo.Orders", "FK_Package_ID_ID");
            DropColumn("dbo.Orders", "Type");
            DropColumn("dbo.Orders", "SKU_Package");
        }
    }
}
