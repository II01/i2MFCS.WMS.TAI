namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test18 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Parameters",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 100),
                        Value = c.String(maxLength: 250),
                    })
                .PrimaryKey(t => t.Name);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Parameters");
        }
    }
}
