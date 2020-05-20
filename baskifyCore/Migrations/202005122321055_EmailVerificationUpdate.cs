namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EmailVerificationUpdate : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.EmailChangeModels", "Username", "dbo.UserModels");
            DropIndex("dbo.EmailChangeModels", new[] { "Username" });
            CreateTable(
                "dbo.EmailVerificationModels",
                c => new
                    {
                        ChangeId = c.Int(nullable: false, identity: true),
                        ChangeType = c.Int(nullable: false),
                        Username = c.String(nullable: false, maxLength: 30),
                        Payload = c.String(nullable: false),
                        ChangeTime = c.DateTime(nullable: false),
                        RevertId = c.Guid(nullable: false),
                        CanRevert = c.Boolean(nullable: false),
                        CommitId = c.Guid(nullable: false),
                        Committed = c.Boolean(nullable: false),
                        Reverted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ChangeId)
                .ForeignKey("dbo.UserModels", t => t.Username, cascadeDelete: true)
                .Index(t => t.Username);
            
            DropTable("dbo.EmailChangeModels");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.EmailChangeModels",
                c => new
                    {
                        EmailChangeId = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 30),
                        OriginalEmail = c.String(nullable: false),
                        NewEmail = c.String(nullable: false),
                        ChangeTime = c.DateTime(nullable: false),
                        RevertId = c.Guid(nullable: false),
                        CommitId = c.Guid(nullable: false),
                        Committed = c.Boolean(nullable: false),
                        Reverted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.EmailChangeId);
            
            DropForeignKey("dbo.EmailVerificationModels", "Username", "dbo.UserModels");
            DropIndex("dbo.EmailVerificationModels", new[] { "Username" });
            DropTable("dbo.EmailVerificationModels");
            CreateIndex("dbo.EmailChangeModels", "Username");
            AddForeignKey("dbo.EmailChangeModels", "Username", "dbo.UserModels", "Username", cascadeDelete: true);
        }
    }
}
