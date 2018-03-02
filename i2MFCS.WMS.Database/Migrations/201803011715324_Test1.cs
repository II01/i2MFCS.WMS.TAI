namespace i2MFCS.WMS.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Test1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PlaceIDs", "PositionTravel", c => c.Double(nullable: false));
            AddColumn("dbo.PlaceIDs", "PositionHoist", c => c.Double(nullable: false));
            AddColumn("dbo.PlaceIDs", "DimensionClass", c => c.Int(nullable: false));
            AddColumn("dbo.PlaceIDs", "FrequencyClass", c => c.Int(nullable: false));
            AddColumn("dbo.SKU_ID", "FrequencyClass", c => c.Int(nullable: false));
            DropColumn("dbo.PlaceIDs", "Size");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PlaceIDs", "Size", c => c.Int(nullable: false));
            DropColumn("dbo.SKU_ID", "FrequencyClass");
            DropColumn("dbo.PlaceIDs", "FrequencyClass");
            DropColumn("dbo.PlaceIDs", "DimensionClass");
            DropColumn("dbo.PlaceIDs", "PositionHoist");
            DropColumn("dbo.PlaceIDs", "PositionTravel");
        }
    }
}
