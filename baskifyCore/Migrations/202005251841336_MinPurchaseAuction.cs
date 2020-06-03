namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MinPurchaseAuction : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "MinPurchase", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "MinPurchase");
        }
    }
}
