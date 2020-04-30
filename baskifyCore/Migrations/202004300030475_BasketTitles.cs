namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BasketTitles : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "BasketTitle", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.BasketModels", "BasketTitle");
        }
    }
}
