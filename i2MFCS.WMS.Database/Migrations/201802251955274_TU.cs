namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TU : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TUs", "Size", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TUs", "Size");
        }
    }
}
