namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class removebearer : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.BearerTokenModels", "Username", "dbo.UserModels");
            DropIndex("dbo.BearerTokenModels", new[] { "Username" });
            AddColumn("dbo.UserModels", "BearerHash", c => c.String());
            DropTable("dbo.BearerTokenModels");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.BearerTokenModels",
                c => new
                    {
                        TokenIdentifier = c.String(nullable: false, maxLength: 128),
                        TimeWritten = c.DateTime(nullable: false),
                        TimeExpire = c.DateTime(nullable: false),
                        Username = c.String(maxLength: 30),
                        Token = c.String(),
                        Type = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.TokenIdentifier);
            
            DropColumn("dbo.UserModels", "BearerHash");
            CreateIndex("dbo.BearerTokenModels", "Username");
            AddForeignKey("dbo.BearerTokenModels", "Username", "dbo.UserModels", "Username");
        }
    }
}
