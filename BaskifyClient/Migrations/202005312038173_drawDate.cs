namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class drawDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "DrawDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "DrawDate");
        }
    }
}
