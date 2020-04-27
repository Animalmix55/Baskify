namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UserAlertChangeKey : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.UserAlertModels", "Username", "dbo.UserModels");
            DropIndex("dbo.UserAlertModels", new[] { "Username" });
            DropPrimaryKey("dbo.UserAlertModels");
            AddColumn("dbo.UserAlertModels", "AlertType", c => c.String(nullable: false, maxLength: 20));
            AlterColumn("dbo.UserAlertModels", "Username", c => c.String(nullable: false, maxLength: 30));
            AddPrimaryKey("dbo.UserAlertModels", new[] { "Username", "AlertType" });
            CreateIndex("dbo.UserAlertModels", "Username");
            AddForeignKey("dbo.UserAlertModels", "Username", "dbo.UserModels", "Username", cascadeDelete: true);
            DropColumn("dbo.UserAlertModels", "AlertId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserAlertModels", "AlertId", c => c.Int(nullable: false, identity: true));
            DropForeignKey("dbo.UserAlertModels", "Username", "dbo.UserModels");
            DropIndex("dbo.UserAlertModels", new[] { "Username" });
            DropPrimaryKey("dbo.UserAlertModels");
            AlterColumn("dbo.UserAlertModels", "Username", c => c.String(maxLength: 30));
            DropColumn("dbo.UserAlertModels", "AlertType");
            AddPrimaryKey("dbo.UserAlertModels", "AlertId");
            CreateIndex("dbo.UserAlertModels", "Username");
            AddForeignKey("dbo.UserAlertModels", "Username", "dbo.UserModels", "Username");
        }
    }
}
