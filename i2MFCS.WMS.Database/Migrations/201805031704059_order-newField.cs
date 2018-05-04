namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ordernewField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "SubOrderERPID", c => c.Int(nullable: false));
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false, maxLength: 200));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "SubOrderName", c => c.String(nullable: false));
            DropColumn("dbo.Orders", "SubOrderERPID");
        }
    }
}
