namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BasketStatus : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "Status", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.BasketModels", "Status");
        }
    }
}
