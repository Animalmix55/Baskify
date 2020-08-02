namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class basketDeliveryTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BasketModels", "DeliveryTime", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BasketModels", "DeliveryTime");
        }
    }
}
