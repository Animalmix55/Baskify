namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class intEin : DbMigration
    {
        public override void Up()
        {
            
            DropPrimaryKey("dbo.IRSNonProfits");
            AlterColumn("dbo.IRSNonProfits", "EIN", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.IRSNonProfits", "EIN");
            CreateIndex("dbo.IRSNonProfitDocuments", "EIN");
        }
        
        public override void Down()
        {
            DropIndex("dbo.IRSNonProfitDocuments", new[] { "EIN" });
            DropPrimaryKey("dbo.IRSNonProfits");
            AlterColumn("dbo.IRSNonProfitDocuments", "EIN", c => c.String(nullable: false, maxLength: 9));
            AlterColumn("dbo.IRSNonProfits", "EIN", c => c.String(nullable: false, maxLength: 9));
            AddPrimaryKey("dbo.IRSNonProfits", "EIN");
            CreateIndex("dbo.IRSNonProfitDocuments", "EIN");
            AddForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits", "EIN", cascadeDelete: true);
        }
    }
}
