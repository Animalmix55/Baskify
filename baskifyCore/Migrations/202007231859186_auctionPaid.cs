namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class auctionPaid : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "PaidOut", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "PaidOut");
        }
    }
}
