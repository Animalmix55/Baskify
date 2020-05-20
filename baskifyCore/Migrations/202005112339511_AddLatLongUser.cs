namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLatLongUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserModels", "Latitude", c => c.Single(nullable: false));
            AddColumn("dbo.UserModels", "Longitude", c => c.Single(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "Longitude");
            DropColumn("dbo.UserModels", "Latitude");
        }
    }
}
