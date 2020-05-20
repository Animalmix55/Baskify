namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ticketPrice : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "TicketCost", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "TicketCost");
        }
    }
}
