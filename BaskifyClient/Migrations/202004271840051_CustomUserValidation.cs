namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CustomUserValidation : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.UserModels", "OrganizationName", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.UserModels", "OrganizationName", c => c.String(nullable: false));
        }
    }
}
