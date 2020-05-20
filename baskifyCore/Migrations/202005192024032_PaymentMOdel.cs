namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PaymentMOdel : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PaymentModels",
                c => new
                    {
                        PaymentIntentId = c.String(nullable: false, maxLength: 128),
                        Time = c.DateTime(nullable: false),
                        Username = c.String(nullable: false, maxLength: 30),
                        Complete = c.Boolean(nullable: false),
                        Success = c.Boolean(nullable: false),
                        AuctionId = c.Int(nullable: false),
                        Amount = c.Single(nullable: false),
                    })
                .PrimaryKey(t => t.PaymentIntentId)
                .ForeignKey("dbo.AuctionModels", t => t.AuctionId, cascadeDelete: true)
                .ForeignKey("dbo.UserModels", t => t.Username, cascadeDelete: false)
                .Index(t => t.Username)
                .Index(t => t.AuctionId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PaymentModels", "Username", "dbo.UserModels");
            DropForeignKey("dbo.PaymentModels", "AuctionId", "dbo.AuctionModels");
            DropIndex("dbo.PaymentModels", new[] { "AuctionId" });
            DropIndex("dbo.PaymentModels", new[] { "Username" });
            DropTable("dbo.PaymentModels");
        }
    }
}
