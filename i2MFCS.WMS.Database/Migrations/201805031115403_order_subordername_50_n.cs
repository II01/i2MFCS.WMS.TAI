namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class order_subordername_50_n : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false, maxLength: 50));
        }
    }
}
