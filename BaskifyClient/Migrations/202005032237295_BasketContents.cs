namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BasketContents : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "BasketContents", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.BasketModels", "BasketContents");
        }
    }
}
