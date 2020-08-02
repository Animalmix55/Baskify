namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AuctionLink : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuctionLinkModels",
                c => new
                    {
                        AuctionId = c.Int(nullable: false),
                        Link = c.Guid(nullable: false, identity: true),
                    })
                .PrimaryKey(t => t.AuctionId)
                .ForeignKey("dbo.AuctionModels", t => t.AuctionId)
                .Index(t => t.AuctionId);
            
            AddColumn("dbo.BasketModels", "Draft", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AuctionLinkModels", "AuctionId", "dbo.AuctionModels");
            DropIndex("dbo.AuctionLinkModels", new[] { "AuctionId" });
            DropColumn("dbo.BasketModels", "Draft");
            DropTable("dbo.AuctionLinkModels");
        }
    }
}
