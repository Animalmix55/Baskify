namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveUserOrgVerified : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.UserModels", "OrganizationVerified");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserModels", "OrganizationVerified", c => c.Boolean(nullable: false));
        }
    }
}
