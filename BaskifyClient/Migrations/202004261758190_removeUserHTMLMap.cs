namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class removeUserHTMLMap : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.UserModels", "userLocationMapHTML");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserModels", "userLocationMapHTML", c => c.String(nullable: false));
        }
    }
}
