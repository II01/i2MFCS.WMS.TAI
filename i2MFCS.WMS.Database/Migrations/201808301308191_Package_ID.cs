namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Package_ID : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Package_ID",
                c => new
                    {
                        ID = c.String(nullable: false, maxLength: 30),
                        SKU_ID = c.String(nullable: false, maxLength: 30),
                        Batch = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.SKU_ID", t => t.SKU_ID, cascadeDelete: true)
                .Index(t => t.SKU_ID)
                .Index(t => t.Batch);
            
            AddColumn("dbo.TUs", "Package_ID", c => c.String(nullable: false, maxLength: 50));
            AddColumn("dbo.TUs", "FK_Package_ID_ID", c => c.String(maxLength: 30));
            AddColumn("dbo.SKU_ID", "Length", c => c.Int(nullable: false));
            AddColumn("dbo.SKU_ID", "Width", c => c.Int(nullable: false));
            AddColumn("dbo.SKU_ID", "Height", c => c.Int(nullable: false));
            CreateIndex("dbo.TUs", "Package_ID");
            CreateIndex("dbo.TUs", "FK_Package_ID_ID");
            AddForeignKey("dbo.TUs", "FK_Package_ID_ID", "dbo.Package_ID", "ID");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.TUs", "FK_Package_ID_ID", "dbo.Package_ID");
            DropForeignKey("dbo.Package_ID", "SKU_ID", "dbo.SKU_ID");
            DropIndex("dbo.Package_ID", new[] { "Batch" });
            DropIndex("dbo.Package_ID", new[] { "SKU_ID" });
            DropIndex("dbo.TUs", new[] { "FK_Package_ID_ID" });
            DropIndex("dbo.TUs", new[] { "Package_ID" });
            DropColumn("dbo.SKU_ID", "Height");
            DropColumn("dbo.SKU_ID", "Width");
            DropColumn("dbo.SKU_ID", "Length");
            DropColumn("dbo.TUs", "FK_Package_ID_ID");
            DropColumn("dbo.TUs", "Package_ID");
            DropTable("dbo.Package_ID");
        }
    }
}
