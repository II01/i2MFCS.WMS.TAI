namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class subordername_15_50 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false, maxLength: 50));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false, maxLength: 15));
        }
    }
}
