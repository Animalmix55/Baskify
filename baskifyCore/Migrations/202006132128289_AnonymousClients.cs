namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AnonymousClients : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AnonymousClientModels",
                c => new
                    {
                        ClientId = c.Guid(nullable: false),
                        Created = c.DateTime(nullable: false),
                        IPAddress = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.ClientId);
            
            AddColumn("dbo.VerificationCodeModels", "AnonymousClientId", c => c.Guid());
            AddColumn("dbo.VerificationCodeModels", "Username", c => c.String(maxLength: 30));
            CreateIndex("dbo.VerificationCodeModels", "AnonymousClientId");
            CreateIndex("dbo.VerificationCodeModels", "Username");
            AddForeignKey("dbo.VerificationCodeModels", "AnonymousClientId", "dbo.AnonymousClientModels", "ClientId");
            AddForeignKey("dbo.VerificationCodeModels", "Username", "dbo.UserModels", "Username");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.VerificationCodeModels", "Username", "dbo.UserModels");
            DropForeignKey("dbo.VerificationCodeModels", "AnonymousClientId", "dbo.AnonymousClientModels");
            DropIndex("dbo.VerificationCodeModels", new[] { "Username" });
            DropIndex("dbo.VerificationCodeModels", new[] { "AnonymousClientId" });
            DropColumn("dbo.VerificationCodeModels", "Username");
            DropColumn("dbo.VerificationCodeModels", "AnonymousClientId");
            DropTable("dbo.AnonymousClientModels");
        }
    }
}
