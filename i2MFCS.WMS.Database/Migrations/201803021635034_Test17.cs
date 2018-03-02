namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test17 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false, maxLength: 15));
            AlterColumn("dbo.Orders", "SKU_Batch", c => c.String(nullable: false, maxLength: 15));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "SKU_Batch", c => c.String(nullable: false));
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false));
        }
    }
}
