namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addUserAlert : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserAlertModels",
                c => new
                    {
                        AlertId = c.Int(nullable: false, identity: true),
                        AlertHeader = c.String(maxLength: 20),
                        AlertBody = c.String(nullable: false, maxLength: 200),
                        Username = c.String(maxLength: 30),
                    })
                .PrimaryKey(t => t.AlertId)
                .ForeignKey("dbo.UserModels", t => t.Username)
                .Index(t => t.Username);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserAlertModels", "Username", "dbo.UserModels");
            DropIndex("dbo.UserAlertModels", new[] { "Username" });
            DropTable("dbo.UserAlertModels");
        }
    }
}
