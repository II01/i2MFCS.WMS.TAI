namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CommandERP1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CommandERPs", "ERP_ID", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CommandERPs", "ERP_ID");
        }
    }
}
