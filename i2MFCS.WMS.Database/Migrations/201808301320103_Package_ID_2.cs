namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Package_ID_2 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.TUs", new[] { "Package_ID" });
            DropIndex("dbo.TUs", new[] { "FK_Package_ID_ID" });
            DropColumn("dbo.TUs", "Package_ID");
            RenameColumn(table: "dbo.TUs", name: "FK_Package_ID_ID", newName: "Package_ID");
            AlterColumn("dbo.TUs", "Package_ID", c => c.String(maxLength: 30));
            CreateIndex("dbo.TUs", "Package_ID");
        }
        
        public override void Down()
        {
            DropIndex("dbo.TUs", new[] { "Package_ID" });
            AlterColumn("dbo.TUs", "Package_ID", c => c.String(nullable: false, maxLength: 50));
            RenameColumn(table: "dbo.TUs", name: "Package_ID", newName: "FK_Package_ID_ID");
            AddColumn("dbo.TUs", "Package_ID", c => c.String(nullable: false, maxLength: 50));
            CreateIndex("dbo.TUs", "FK_Package_ID_ID");
            CreateIndex("dbo.TUs", "Package_ID");
        }
    }
}
