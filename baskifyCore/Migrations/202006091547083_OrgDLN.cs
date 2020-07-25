namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OrgDLN : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.IRSNonProfitIndexModels");
            AddColumn("dbo.IRSNonProfitIndexModels", "DLN", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.IRSNonProfitIndexModels", "TaxPeriod", c => c.String(nullable: false));
            AlterColumn("dbo.IRSNonProfitIndexModels", "EIN", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.IRSNonProfitIndexModels", "DLN");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.IRSNonProfitIndexModels");
            AlterColumn("dbo.IRSNonProfitIndexModels", "EIN", c => c.String(nullable: false, maxLength: 128));
            DropColumn("dbo.IRSNonProfitIndexModels", "TaxPeriod");
            DropColumn("dbo.IRSNonProfitIndexModels", "DLN");
            AddPrimaryKey("dbo.IRSNonProfitIndexModels", "EIN");
        }
    }
}
