namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UsedVerifications : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.VerificationCodeModels", "Used", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.VerificationCodeModels", "Used");
        }
    }
}
