namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BasketDraw : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "WinnerUsername", c => c.String(maxLength: 30));
            CreateIndex("dbo.BasketModels", "WinnerUsername");
            AddForeignKey("dbo.BasketModels", "WinnerUsername", "dbo.UserModels", "Username");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.BasketModels", "WinnerUsername", "dbo.UserModels");
            DropIndex("dbo.BasketModels", new[] { "WinnerUsername" });
            DropColumn("dbo.BasketModels", "WinnerUsername");
        }
    }
}
