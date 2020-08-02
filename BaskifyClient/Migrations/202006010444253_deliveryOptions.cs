namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class deliveryOptions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "DeliveredBySubmittingUser", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "DeliveredBySubmittingUser");
        }
    }
}
