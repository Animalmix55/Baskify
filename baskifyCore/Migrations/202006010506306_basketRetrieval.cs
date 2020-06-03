namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class basketRetrieval : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "BasketRetrieval", c => c.Int());
            DropColumn("dbo.AuctionModels", "DeliveredBySubmittingUser");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AuctionModels", "DeliveredBySubmittingUser", c => c.Boolean());
            DropColumn("dbo.AuctionModels", "BasketRetrieval");
        }
    }
}
