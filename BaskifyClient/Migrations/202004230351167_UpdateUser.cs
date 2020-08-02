namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateUser : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.UserModels");
            AddColumn("dbo.UserModels", "Username", c => c.String(nullable: false, maxLength: 30));
            AddColumn("dbo.UserModels", "PasswordHash", c => c.String());
            AddColumn("dbo.UserModels", "bearerToken", c => c.String());
            AlterColumn("dbo.UserModels", "Id", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.UserModels", "Username");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.UserModels");
            AlterColumn("dbo.UserModels", "Id", c => c.Int(nullable: false, identity: true));
            DropColumn("dbo.UserModels", "bearerToken");
            DropColumn("dbo.UserModels", "PasswordHash");
            DropColumn("dbo.UserModels", "Username");
            AddPrimaryKey("dbo.UserModels", "Id");
        }
    }
}
