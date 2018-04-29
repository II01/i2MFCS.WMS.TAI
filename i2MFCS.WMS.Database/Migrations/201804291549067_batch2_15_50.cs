namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class batch2_15_50 : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.TUs", new[] { "Batch" });
            AlterColumn("dbo.TUs", "Batch", c => c.String(nullable: false, maxLength: 50));
            CreateIndex("dbo.TUs", "Batch");
        }
        
        public override void Down()
        {
            DropIndex("dbo.TUs", new[] { "Batch" });
            AlterColumn("dbo.TUs", "Batch", c => c.String(nullable: false, maxLength: 15));
            CreateIndex("dbo.TUs", "Batch");
        }
    }
}
