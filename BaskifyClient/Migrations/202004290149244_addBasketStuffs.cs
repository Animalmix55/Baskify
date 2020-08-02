namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addBasketStuffs : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuctionModels",
                c => new
                    {
                        AuctionId = c.Int(nullable: false, identity: true),
                        IsLive = c.Boolean(nullable: false),
                        HostUsername = c.String(nullable: false, maxLength: 30),
                        EndTime = c.DateTime(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.AuctionId)
                .ForeignKey("dbo.UserModels", t => t.HostUsername, cascadeDelete: true)
                .Index(t => t.HostUsername);
            
            CreateTable(
                "dbo.BasketModels",
                c => new
                    {
                        BasketId = c.Int(nullable: false, identity: true),
                        SubmittingUsername = c.String(maxLength: 30),
                        BasketDescription = c.String(nullable: false),
                        AcceptedByOrg = c.Boolean(nullable: false),
                        SubmissionDate = c.DateTime(nullable: false),
                        AuctionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.BasketId)
                .ForeignKey("dbo.AuctionModels", t => t.AuctionId, cascadeDelete: true)
                .ForeignKey("dbo.UserModels", t => t.SubmittingUsername)
                .Index(t => t.SubmittingUsername)
                .Index(t => t.AuctionId);
            
            CreateTable(
                "dbo.BasketPhotoModels",
                c => new
                    {
                        PhotoId = c.Int(nullable: false, identity: true),
                        Url = c.String(nullable: false),
                        PhotoDesc = c.String(),
                        BasketId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PhotoId)
                .ForeignKey("dbo.BasketModels", t => t.BasketId, cascadeDelete: true)
                .Index(t => t.BasketId);
            
            CreateTable(
                "dbo.TicketModels",
                c => new
                    {
                        Username = c.String(nullable: false, maxLength: 30),
                        BasketId = c.Int(nullable: false),
                        NumTickets = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Username, t.BasketId })
                .ForeignKey("dbo.BasketModels", t => t.BasketId, cascadeDelete: false)
                .ForeignKey("dbo.UserModels", t => t.Username, cascadeDelete: true)
                .Index(t => t.Username)
                .Index(t => t.BasketId);
            
            CreateTable(
                "dbo.UserAuctionWalletModels",
                c => new
                    {
                        Username = c.String(nullable: false, maxLength: 30),
                        AuctionId = c.Int(nullable: false),
                        WalletBalance = c.Double(nullable: false),
                    })
                .PrimaryKey(t => new { t.Username, t.AuctionId })
                .ForeignKey("dbo.AuctionModels", t => t.AuctionId, cascadeDelete: false)
                .ForeignKey("dbo.UserModels", t => t.Username, cascadeDelete: true)
                .Index(t => t.Username)
                .Index(t => t.AuctionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserAuctionWalletModels", "Username", "dbo.UserModels");
            DropForeignKey("dbo.UserAuctionWalletModels", "AuctionId", "dbo.AuctionModels");
            DropForeignKey("dbo.AuctionModels", "HostUsername", "dbo.UserModels");
            DropForeignKey("dbo.TicketModels", "Username", "dbo.UserModels");
            DropForeignKey("dbo.TicketModels", "BasketId", "dbo.BasketModels");
            DropForeignKey("dbo.BasketModels", "SubmittingUsername", "dbo.UserModels");
            DropForeignKey("dbo.BasketPhotoModels", "BasketId", "dbo.BasketModels");
            DropForeignKey("dbo.BasketModels", "AuctionId", "dbo.AuctionModels");
            DropIndex("dbo.UserAuctionWalletModels", new[] { "AuctionId" });
            DropIndex("dbo.UserAuctionWalletModels", new[] { "Username" });
            DropIndex("dbo.TicketModels", new[] { "BasketId" });
            DropIndex("dbo.TicketModels", new[] { "Username" });
            DropIndex("dbo.BasketPhotoModels", new[] { "BasketId" });
            DropIndex("dbo.BasketModels", new[] { "AuctionId" });
            DropIndex("dbo.BasketModels", new[] { "SubmittingUsername" });
            DropIndex("dbo.AuctionModels", new[] { "HostUsername" });
            DropTable("dbo.UserAuctionWalletModels");
            DropTable("dbo.TicketModels");
            DropTable("dbo.BasketPhotoModels");
            DropTable("dbo.BasketModels");
            DropTable("dbo.AuctionModels");
        }
    }
}
