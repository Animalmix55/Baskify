namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Counties : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.AuctionInStateModels", "AuctionId", "dbo.AuctionModels");
            DropForeignKey("dbo.AuctionInStateModels", "StateAbbrv", "dbo.StateModels");
            DropIndex("dbo.AuctionInStateModels", new[] { "StateAbbrv" });
            DropIndex("dbo.AuctionInStateModels", new[] { "AuctionId" });
            DropPrimaryKey("dbo.StateModels");
            CreateTable(
                "dbo.AuctionInCountyModels",
                c => new
                    {
                        StateName = c.String(nullable: false, maxLength: 128),
                        CountyName = c.String(nullable: false, maxLength: 128),
                        AuctionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.StateName, t.CountyName, t.AuctionId })
                .ForeignKey("dbo.AuctionModels", t => t.AuctionId, cascadeDelete: true)
                .ForeignKey("dbo.CountyModels", t => new { t.StateName, t.CountyName }, cascadeDelete: true)
                .Index(t => new { t.StateName, t.CountyName })
                .Index(t => t.AuctionId);
            
            CreateTable(
                "dbo.CountyModels",
                c => new
                    {
                        CountyName = c.String(nullable: false, maxLength: 128),
                        StateName = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.CountyName, t.StateName })
                .ForeignKey("dbo.StateModels", t => t.StateName, cascadeDelete: true)
                .Index(t => t.StateName);
            
            AddColumn("dbo.UserModels", "County", c => c.String());
            AlterColumn("dbo.StateModels", "Abbrv", c => c.String(nullable: false));
            AlterColumn("dbo.StateModels", "FullName", c => c.String(nullable: false, maxLength: 128));
            AddPrimaryKey("dbo.StateModels", "FullName");
            DropTable("dbo.AuctionInStateModels");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.AuctionInStateModels",
                c => new
                    {
                        StateAbbrv = c.String(nullable: false, maxLength: 128),
                        AuctionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.StateAbbrv, t.AuctionId });
            
            DropForeignKey("dbo.AuctionInCountyModels", new[] { "StateName", "CountyName" }, "dbo.CountyModels");
            DropForeignKey("dbo.CountyModels", "StateName", "dbo.StateModels");
            DropForeignKey("dbo.AuctionInCountyModels", "AuctionId", "dbo.AuctionModels");
            DropIndex("dbo.CountyModels", new[] { "StateName" });
            DropIndex("dbo.AuctionInCountyModels", new[] { "AuctionId" });
            DropIndex("dbo.AuctionInCountyModels", new[] { "StateName", "CountyName" });
            DropPrimaryKey("dbo.StateModels");
            AlterColumn("dbo.StateModels", "FullName", c => c.String(nullable: false));
            AlterColumn("dbo.StateModels", "Abbrv", c => c.String(nullable: false, maxLength: 128));
            DropColumn("dbo.UserModels", "County");
            DropTable("dbo.CountyModels");
            DropTable("dbo.AuctionInCountyModels");
            AddPrimaryKey("dbo.StateModels", "Abbrv");
            CreateIndex("dbo.AuctionInStateModels", "AuctionId");
            CreateIndex("dbo.AuctionInStateModels", "StateAbbrv");
            AddForeignKey("dbo.AuctionInStateModels", "StateAbbrv", "dbo.StateModels", "Abbrv", cascadeDelete: true);
            AddForeignKey("dbo.AuctionInStateModels", "AuctionId", "dbo.AuctionModels", "AuctionId", cascadeDelete: true);
        }
    }
}
