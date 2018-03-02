namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test12 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "ReleaseTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.CommandERPs", "Command", c => c.String(nullable: false));
            AddColumn("dbo.TU_ID", "DimensionClass", c => c.Int(nullable: false));
            AddColumn("dbo.TU_ID", "Blocked", c => c.Int(nullable: false));
            AddColumn("dbo.TUs", "Batch", c => c.String(nullable: false));
            AddColumn("dbo.TUs", "ProdDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.TUs", "ExpDate", c => c.DateTime(nullable: false));
            DropColumn("dbo.CommandERPs", "CommandDecription");
            DropColumn("dbo.TU_ID", "Size");
        }
        
        public override void Down()
        {
            AddColumn("dbo.TU_ID", "Size", c => c.Int(nullable: false));
            AddColumn("dbo.CommandERPs", "CommandDecription", c => c.String(nullable: false));
            DropColumn("dbo.TUs", "ExpDate");
            DropColumn("dbo.TUs", "ProdDate");
            DropColumn("dbo.TUs", "Batch");
            DropColumn("dbo.TU_ID", "Blocked");
            DropColumn("dbo.TU_ID", "DimensionClass");
            DropColumn("dbo.CommandERPs", "Command");
            DropColumn("dbo.Orders", "ReleaseTime");
        }
    }
}
