namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTokens : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BearerTokenModels",
                c => new
                    {
                        TokenId = c.Int(nullable: false, identity: true),
                        TimeWritten = c.DateTime(nullable: false),
                        Username = c.String(maxLength: 30),
                        Token = c.String(),
                    })
                .PrimaryKey(t => t.TokenId)
                .ForeignKey("dbo.UserModels", t => t.Username)
                .Index(t => t.Username);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.BearerTokenModels", "Username", "dbo.UserModels");
            DropIndex("dbo.BearerTokenModels", new[] { "Username" });
            DropTable("dbo.BearerTokenModels");
        }
    }
}
