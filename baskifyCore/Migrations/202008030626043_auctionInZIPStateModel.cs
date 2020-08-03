namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class auctionInZIPStateModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionInZIPs", "City", c => c.String());
            AddColumn("dbo.AuctionInZIPs", "State", c => c.String(maxLength: 128));
            CreateIndex("dbo.AuctionInZIPs", "State");
            AddForeignKey("dbo.AuctionInZIPs", "State", "dbo.StateModels", "FullName");
            DropColumn("dbo.AuctionInZIPs", "Details");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AuctionInZIPs", "Details", c => c.String());
            DropForeignKey("dbo.AuctionInZIPs", "State", "dbo.StateModels");
            DropIndex("dbo.AuctionInZIPs", new[] { "State" });
            DropColumn("dbo.AuctionInZIPs", "State");
            DropColumn("dbo.AuctionInZIPs", "City");
        }
    }
}
