namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AuctionRemoveLive : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.AuctionModels", "IsLive");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AuctionModels", "IsLive", c => c.Boolean(nullable: false));
        }
    }
}
