namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NonprofitDb : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IRSNonProfitIndexModels",
                c => new
                    {
                        EIN = c.String(nullable: false, maxLength: 128),
                        OrganizationName = c.String(nullable: false),
                        URL = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.EIN);
            
            AddColumn("dbo.BasketModels", "Delivered", c => c.Boolean(nullable: false));
            AddColumn("dbo.BasketModels", "TrackingNumber", c => c.String());
            AddColumn("dbo.BasketModels", "Carrier", c => c.Int());
            DropColumn("dbo.BasketModels", "Status");
        }
        
        public override void Down()
        {
            AddColumn("dbo.BasketModels", "Status", c => c.Int(nullable: false));
            DropColumn("dbo.BasketModels", "Carrier");
            DropColumn("dbo.BasketModels", "TrackingNumber");
            DropColumn("dbo.BasketModels", "Delivered");
            DropTable("dbo.IRSNonProfitIndexModels");
        }
    }
}
