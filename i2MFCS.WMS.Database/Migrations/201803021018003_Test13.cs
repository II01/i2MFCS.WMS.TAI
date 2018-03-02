namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test13 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Orders", "CustomerID", "dbo.Customers");
            DropIndex("dbo.Orders", new[] { "CustomerID" });
            AddColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false));
            AddColumn("dbo.Orders", "SKU_Qty", c => c.Double(nullable: false));
            AddColumn("dbo.Orders", "SKU_Batch", c => c.String(nullable: false));
            DropColumn("dbo.Orders", "CustomerID");
            DropColumn("dbo.Orders", "Qty");
            DropColumn("dbo.Orders", "Sequence");
            DropTable("dbo.Customers");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.Customers",
                c => new
                    {
                        ID = c.Int(nullable: false),
                        Name = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.ID);
            
            AddColumn("dbo.Orders", "Sequence", c => c.Int(nullable: false));
            AddColumn("dbo.Orders", "Qty", c => c.Double(nullable: false));
            AddColumn("dbo.Orders", "CustomerID", c => c.Int(nullable: false));
            DropColumn("dbo.Orders", "SKU_Batch");
            DropColumn("dbo.Orders", "SKU_Qty");
            DropColumn("dbo.Orders", "SubOrderName");
            CreateIndex("dbo.Orders", "CustomerID");
            AddForeignKey("dbo.Orders", "CustomerID", "dbo.Customers", "ID", cascadeDelete: true);
        }
    }
}
