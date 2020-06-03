namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BasketDraw2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "DeliveryType", c => c.Int(nullable: false));
            AddColumn("dbo.UserModels", "ContactEmail", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "ContactEmail");
            DropColumn("dbo.AuctionModels", "DeliveryType");
        }
    }
}
