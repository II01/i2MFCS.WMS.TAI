namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test16 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.TUs", "Qty", c => c.Double(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.TUs", "Qty", c => c.Int(nullable: false));
        }
    }
}
