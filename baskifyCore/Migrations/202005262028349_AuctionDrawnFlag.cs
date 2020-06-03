namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AuctionDrawnFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "isDrawn", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "isDrawn");
        }
    }
}
