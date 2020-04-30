namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddOrganizationFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserModels", "EmailVerified", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.UserModels", "OrganizationName", c => c.String(nullable: true));
            AddColumn("dbo.UserModels", "OrganizationVerified", c => c.Boolean(nullable: false, defaultValue: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "OrganizationVerified");
            DropColumn("dbo.UserModels", "OrganizationName");
            DropColumn("dbo.UserModels", "EmailVerified");
        }
    }
}
