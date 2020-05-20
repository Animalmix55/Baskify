namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MaxRange : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AuctionModels", "MaxRange", c => c.Int(nullable: false, defaultValue: 30));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AuctionModels", "MaxRange");
        }
    }
}
