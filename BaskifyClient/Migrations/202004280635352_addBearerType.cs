namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addBearerType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BearerTokenModels", "Type", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.BearerTokenModels", "Type");
        }
    }
}
