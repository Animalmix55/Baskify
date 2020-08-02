namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class removeUserID : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.UserModels", "Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserModels", "Id", c => c.Int(nullable: false));
        }
    }
}
