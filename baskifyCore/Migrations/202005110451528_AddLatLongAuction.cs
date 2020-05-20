namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLatLongAuction : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "Latitude", c => c.Single(nullable: false));
            AddColumn("dbo.AuctionModels", "Longitude", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "Longitude");
            DropColumn("dbo.AuctionModels", "Latitude");
        }
    }
}
