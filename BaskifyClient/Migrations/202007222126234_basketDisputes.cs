namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class basketDisputes : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "DisputedShipment", c => c.Boolean(nullable: false));
            AddColumn("dbo.BasketModels", "DisputeReason", c => c.String());
            AddColumn("dbo.BasketModels", "DisputeTime", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BasketModels", "DisputeTime");
            DropColumn("dbo.BasketModels", "DisputeReason");
            DropColumn("dbo.BasketModels", "DisputedShipment");
        }
    }
}
