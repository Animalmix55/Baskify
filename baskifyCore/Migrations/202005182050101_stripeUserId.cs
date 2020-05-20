namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class stripeUserId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserModels", "StripeCustomerId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "StripeCustomerId");
        }
    }
}
