namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class batch_15_50 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "SKU_Batch", c => c.String(nullable: false, maxLength: 50));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "SKU_Batch", c => c.String(nullable: false, maxLength: 15));
        }
    }
}
