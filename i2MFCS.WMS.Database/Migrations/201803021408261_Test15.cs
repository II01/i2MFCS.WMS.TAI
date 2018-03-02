namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test15 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs");
            DropPrimaryKey("dbo.CommandERPs");
            AlterColumn("dbo.CommandERPs", "ID", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.CommandERPs", "ID");
            AddForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs", "ID", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs");
            DropPrimaryKey("dbo.CommandERPs");
            AlterColumn("dbo.CommandERPs", "ID", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.CommandERPs", "ID");
            AddForeignKey("dbo.Orders", "ERP_ID", "dbo.CommandERPs", "ID", cascadeDelete: true);
        }
    }
}
