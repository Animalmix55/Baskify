namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddOrganizationFields2 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.UserModels", "FirstName", c => c.String());
            AlterColumn("dbo.UserModels", "LastName", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.UserModels", "LastName", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "FirstName", c => c.String(nullable: false));
        }
    }
}
