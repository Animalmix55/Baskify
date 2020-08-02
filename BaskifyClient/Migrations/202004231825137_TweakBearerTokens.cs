namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TweakBearerTokens : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.BearerTokenModels");
            AddColumn("dbo.BearerTokenModels", "TokenGuid", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.BearerTokenModels", "TokenGuid");
            DropColumn("dbo.UserModels", "bearerToken");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserModels", "bearerToken", c => c.String());
            DropPrimaryKey("dbo.BearerTokenModels");
            DropColumn("dbo.BearerTokenModels", "TokenGuid");
            AddPrimaryKey("dbo.BearerTokenModels", "TokenId");
        }
    }
}
