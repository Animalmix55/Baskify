namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DocumentEinForeignKey : DbMigration
    {
        public override void Up()
        {
            AddForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits", "EIN", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits");
        }
    }
}
