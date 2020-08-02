namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OrgAddPeriodAndType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.IRSNonProfitIndexModels", "TaxPeriod", c => c.String());
            AddColumn("dbo.IRSNonProfitIndexModels", "FormType", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.IRSNonProfitIndexModels", "FormType");
            DropColumn("dbo.IRSNonProfitIndexModels", "TaxPeriod");
        }
    }
}
