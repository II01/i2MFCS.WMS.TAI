namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTimeToPlace : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Commands", "Time", c => c.DateTime(nullable: false, defaultValueSql:"GETDATE()"));
            AddColumn("dbo.Places", "Time", c => c.DateTime(nullable: false, defaultValueSql:"GETDATE()"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Places", "Time");
            DropColumn("dbo.Commands", "Time");
        }
    }
}
