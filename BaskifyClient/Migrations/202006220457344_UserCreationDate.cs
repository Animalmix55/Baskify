namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UserCreationDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserModels", "CreationDate", c => c.DateTime(nullable: false));
            DropColumn("dbo.UserModels", "DateOfBirth");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserModels", "DateOfBirth", c => c.DateTime(nullable: false));
            DropColumn("dbo.UserModels", "CreationDate");
        }
    }
}
