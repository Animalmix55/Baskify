namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class paymentIncludesFeeInCents : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentModels", "Fee", c => c.Int(nullable: false));
            AlterColumn("dbo.PaymentModels", "Amount", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PaymentModels", "Amount", c => c.Single(nullable: false));
            DropColumn("dbo.PaymentModels", "Fee");
        }
    }
}
