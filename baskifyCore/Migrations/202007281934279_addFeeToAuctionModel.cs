namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addFeeToAuctionModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "FeePerTrans", c => c.Int(nullable: false));
            AddColumn("dbo.AuctionModels", "FeePercentage", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "FeePercentage");
            DropColumn("dbo.AuctionModels", "FeePerTrans");
        }
    }
}
