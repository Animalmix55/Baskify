namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class updateUserHTMLMap : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserModels", "userLocationMapHTML", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "userLocationMapHTML");
        }
    }
}
