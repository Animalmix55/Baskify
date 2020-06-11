namespace baskifyCore.Migrations
{
    using Stripe;
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SplitNonProfitNameandDocs : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.IRSNonProfitIndexModels", newName: "IRSNonProfitDocuments");
            CreateTable(
                "dbo.IRSNonProfits",
                c => new
                    {
                        EIN = c.String(nullable: false, maxLength: 9),
                        OrganizationName = c.String(nullable: false, maxLength: 120),
                        City = c.String(nullable: false, maxLength: 60),
                        State = c.String(nullable: false, maxLength: 2),
                        Country = c.String(nullable: false, maxLength: 100),
                })
                .PrimaryKey(t => t.EIN);
            
            AlterColumn("dbo.IRSNonProfitDocuments", "EIN", c => c.String(nullable: false, maxLength: 9));
            CreateIndex("dbo.IRSNonProfitDocuments", "EIN");
            AddForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits", "EIN", cascadeDelete: true);
            DropColumn("dbo.IRSNonProfitDocuments", "OrganizationName");
        }
        
        public override void Down()
        {
            AddColumn("dbo.IRSNonProfitDocuments", "OrganizationName", c => c.String(nullable: false));
            DropForeignKey("dbo.IRSNonProfitDocuments", "EIN", "dbo.IRSNonProfits");
            DropIndex("dbo.IRSNonProfitDocuments", new[] { "EIN" });
            AlterColumn("dbo.IRSNonProfitDocuments", "EIN", c => c.String(nullable: false));
            DropTable("dbo.IRSNonProfits");
            RenameTable(name: "dbo.IRSNonProfitDocuments", newName: "IRSNonProfitIndexModels");
        }
    }
}
