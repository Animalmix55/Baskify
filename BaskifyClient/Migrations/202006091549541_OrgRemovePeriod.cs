namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OrgRemovePeriod : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.IRSNonProfitIndexModels", "TaxPeriod");
        }
        
        public override void Down()
        {
            AddColumn("dbo.IRSNonProfitIndexModels", "TaxPeriod", c => c.String(nullable: false));
        }
    }
}
