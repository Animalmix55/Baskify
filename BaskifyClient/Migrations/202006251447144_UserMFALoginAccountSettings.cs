namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UserMFALoginAccountSettings : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.VerificationCodeModels", "PhoneNumber", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.VerificationCodeModels", "PhoneNumber");
        }
    }
}
