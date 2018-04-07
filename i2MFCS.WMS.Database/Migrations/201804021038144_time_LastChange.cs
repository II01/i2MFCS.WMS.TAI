namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class time_LastChange : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CommandERPs", "LastChange", c => c.DateTime(nullable: false));
            AddColumn("dbo.Commands", "LastChange", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Commands", "LastChange");
            DropColumn("dbo.CommandERPs", "LastChange");
        }
    }
}
