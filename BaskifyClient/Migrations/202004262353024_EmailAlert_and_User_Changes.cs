namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EmailAlert_and_User_Changes : DbMigration
    {
        public override void Up()
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
                .PrimaryKey(t => t.EmailChangeId)
                .ForeignKey("dbo.UserModels", t => t.Username, cascadeDelete: true)
                .Index(t => t.Username);
            
            DropColumn("dbo.UserModels", "PendingEmail");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserModels", "PendingEmail", c => c.String());
            DropForeignKey("dbo.EmailChangeModels", "Username", "dbo.UserModels");
            DropIndex("dbo.EmailChangeModels", new[] { "Username" });
            DropTable("dbo.EmailChangeModels");
        }
    }
}
