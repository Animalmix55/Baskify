namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MinPurchaseDecimalandMinimumMet : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserAuctionWalletModels", "MinimumMet", c => c.Boolean(nullable: false));
            AlterColumn("dbo.AuctionModels", "TicketCost", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.AuctionModels", "MinPurchase", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.AuctionModels", "MinPurchase", c => c.Single(nullable: false));
            AlterColumn("dbo.AuctionModels", "TicketCost", c => c.Single(nullable: false));
            DropColumn("dbo.UserAuctionWalletModels", "MinimumMet");
        }
    }
}
