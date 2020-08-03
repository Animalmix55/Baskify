namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class auctionInZIP : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuctionInZIPs",
                c => new
                    {
                        AuctionId = c.Int(nullable: false),
                        ZIP = c.String(nullable: false, maxLength: 128),
                        Details = c.String(),
                    })
                .PrimaryKey(t => new { t.AuctionId, t.ZIP })
                .ForeignKey("dbo.AuctionModels", t => t.AuctionId, cascadeDelete: true)
                .Index(t => t.AuctionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AuctionInZIPs", "AuctionId", "dbo.AuctionModels");
            DropIndex("dbo.AuctionInZIPs", new[] { "AuctionId" });
            DropTable("dbo.AuctionInZIPs");
        }
    }
}
