namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updateUserReqs : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.UserModels", "FirstName", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "Email", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "LastName", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "Address", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "City", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "State", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "ZIP", c => c.String(nullable: false));
            AlterColumn("dbo.UserModels", "iconUrl", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.UserModels", "iconUrl", c => c.String());
            AlterColumn("dbo.UserModels", "ZIP", c => c.String());
            AlterColumn("dbo.UserModels", "State", c => c.String());
            AlterColumn("dbo.UserModels", "City", c => c.String());
            AlterColumn("dbo.UserModels", "Address", c => c.String());
            AlterColumn("dbo.UserModels", "LastName", c => c.String());
            AlterColumn("dbo.UserModels", "Email", c => c.String());
            AlterColumn("dbo.UserModels", "FirstName", c => c.String());
        }
    }
}
