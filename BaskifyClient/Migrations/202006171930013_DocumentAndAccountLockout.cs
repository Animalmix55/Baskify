namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DocumentAndAccountLockout : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AccountDocumentsModels",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 30),
                        Document = c.Binary(),
                        UploadDate = c.DateTime(nullable: false),
                        Type = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.UserModels", t => t.Username, cascadeDelete: true)
                .Index(t => t.Username);
            
            AddColumn("dbo.UserModels", "Locked", c => c.Boolean(nullable: false));
            AddColumn("dbo.UserModels", "LockReason", c => c.Int());
            AddColumn("dbo.UserModels", "LockDetails", c => c.String(maxLength: 100));
            AddColumn("dbo.UserModels", "EIN", c => c.Int());
            CreateIndex("dbo.UserModels", "EIN");
            AddForeignKey("dbo.UserModels", "EIN", "dbo.IRSNonProfits", "EIN");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AccountDocumentsModels", "Username", "dbo.UserModels");
            DropForeignKey("dbo.UserModels", "EIN", "dbo.IRSNonProfits");
            DropIndex("dbo.UserModels", new[] { "EIN" });
            DropIndex("dbo.AccountDocumentsModels", new[] { "Username" });
            DropColumn("dbo.UserModels", "EIN");
            DropColumn("dbo.UserModels", "LockDetails");
            DropColumn("dbo.UserModels", "LockReason");
            DropColumn("dbo.UserModels", "Locked");
            DropTable("dbo.AccountDocumentsModels");
        }
    }
}
