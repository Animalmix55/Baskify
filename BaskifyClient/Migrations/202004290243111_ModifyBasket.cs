namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ModifyBasket : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "Address", c => c.String(nullable: false));
            AddColumn("dbo.AuctionModels", "City", c => c.String(nullable: false));
            AddColumn("dbo.AuctionModels", "State", c => c.String(nullable: false));
            AddColumn("dbo.AuctionModels", "ZIP", c => c.String(nullable: false));
            AddColumn("dbo.AuctionModels", "Title", c => c.String(nullable: false));
            AddColumn("dbo.AuctionModels", "Description", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "Description");
            DropColumn("dbo.AuctionModels", "Title");
            DropColumn("dbo.AuctionModels", "ZIP");
            DropColumn("dbo.AuctionModels", "State");
            DropColumn("dbo.AuctionModels", "City");
            DropColumn("dbo.AuctionModels", "Address");
        }
    }
}
