namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class billingInfo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentModels", "CardholderName", c => c.String());
            AddColumn("dbo.PaymentModels", "BillingAddress", c => c.String());
            AddColumn("dbo.PaymentModels", "BillingCity", c => c.String());
            AddColumn("dbo.PaymentModels", "BillingState", c => c.String());
            AddColumn("dbo.PaymentModels", "BillingZIP", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PaymentModels", "BillingZIP");
            DropColumn("dbo.PaymentModels", "BillingState");
            DropColumn("dbo.PaymentModels", "BillingCity");
            DropColumn("dbo.PaymentModels", "BillingAddress");
            DropColumn("dbo.PaymentModels", "CardholderName");
        }
    }
}
