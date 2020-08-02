namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class basketContentsList : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "BasketContentString", c => c.String());
            DropColumn("dbo.BasketModels", "BasketContents");
        }
        
        public override void Down()
        {
            AddColumn("dbo.BasketModels", "BasketContents", c => c.String(nullable: false));
            DropColumn("dbo.BasketModels", "BasketContentString");
        }
    }
}
