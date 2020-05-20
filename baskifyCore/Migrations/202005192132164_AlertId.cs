namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AlertId : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.UserAlertModels");
            AddColumn("dbo.UserAlertModels", "Id", c => c.Guid(nullable: false, identity: true));
            AddColumn("dbo.UserAlertModels", "Dismissable", c => c.Boolean(nullable: false));
            AddColumn("dbo.EmailVerificationModels", "AlertId", c => c.Guid(nullable: true));
            AddPrimaryKey("dbo.UserAlertModels", "Id");
            CreateIndex("dbo.EmailVerificationModels", "AlertId");
            AddForeignKey("dbo.EmailVerificationModels", "AlertId", "dbo.UserAlertModels", "Id", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.EmailVerificationModels", "AlertId", "dbo.UserAlertModels");
            DropIndex("dbo.EmailVerificationModels", new[] { "AlertId" });
            DropPrimaryKey("dbo.UserAlertModels");
            DropColumn("dbo.EmailVerificationModels", "AlertId");
            DropColumn("dbo.UserAlertModels", "Dismissable");
            DropColumn("dbo.UserAlertModels", "Id");
            AddPrimaryKey("dbo.UserAlertModels", new[] { "Username", "AlertType" });
        }
    }
}
