namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class lockPayments : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PaymentModels", "Locked", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PaymentModels", "Locked");
        }
    }
}
