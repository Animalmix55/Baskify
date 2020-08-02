namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class basketContentString : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.BasketModels", "BasketContentString", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.BasketModels", "BasketContentString", c => c.String());
        }
    }
}
