namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DocumentEinForeignKey : DbMigration
    {
        public override void Up()
        {
            try { DropForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits"); } catch (Exception) { }
            AddForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits", "EIN", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits");
        }
    }
}
