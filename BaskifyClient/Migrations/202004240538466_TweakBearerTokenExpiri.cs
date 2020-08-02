namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TweakBearerTokenExpiri : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BearerTokenModels", "TimeExpire", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.BearerTokenModels", "TimeExpire");
        }
    }
}
