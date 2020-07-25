namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StripeRegistrationEmailField : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StripeRegistrationModels", "EmailVerificationId", c => c.Int());
            CreateIndex("dbo.StripeRegistrationModels", "EmailVerificationId");
            AddForeignKey("dbo.StripeRegistrationModels", "EmailVerificationId", "dbo.EmailVerificationModels", "ChangeId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StripeRegistrationModels", "EmailVerificationId", "dbo.EmailVerificationModels");
            DropIndex("dbo.StripeRegistrationModels", new[] { "EmailVerificationId" });
            DropColumn("dbo.StripeRegistrationModels", "EmailVerificationId");
        }
    }
}
