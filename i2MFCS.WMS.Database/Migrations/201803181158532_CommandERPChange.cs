namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CommandERPChange : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CommandERPs", "Reference", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.CommandERPs", "Reference");
        }
    }
}
