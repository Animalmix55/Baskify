namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TweakBearerTokenId : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.BearerTokenModels");
            AddColumn("dbo.BearerTokenModels", "TokenIdentifier", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.BearerTokenModels", "TokenIdentifier");
            DropColumn("dbo.BearerTokenModels", "TokenGuid");
            DropColumn("dbo.BearerTokenModels", "tokenId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.BearerTokenModels", "tokenId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.BearerTokenModels", "TokenGuid", c => c.String(nullable: false, maxLength: 128));
            DropPrimaryKey("dbo.BearerTokenModels");
            DropColumn("dbo.BearerTokenModels", "TokenIdentifier");
            AddPrimaryKey("dbo.BearerTokenModels", "TokenGuid");
        }
    }
}
