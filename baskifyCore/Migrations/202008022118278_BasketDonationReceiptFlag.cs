namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BasketDonationReceiptFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "ReceiptSent", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.BasketModels", "ReceiptSent");
        }
    }
}
