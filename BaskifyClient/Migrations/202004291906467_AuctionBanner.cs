namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AuctionBanner : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "BannerImageUrl", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "BannerImageUrl");
        }
    }
}
