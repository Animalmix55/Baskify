namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CountyId : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.AuctionInCountyModels", new[] { "StateName", "CountyName" }, "dbo.CountyModels");
            DropIndex("dbo.AuctionInCountyModels", new[] { "StateName", "CountyName" });
            RenameColumn(table: "dbo.AuctionInCountyModels", name: "StateName", newName: "CountyId");
            DropPrimaryKey("dbo.AuctionInCountyModels");
            DropPrimaryKey("dbo.CountyModels");
            AddColumn("dbo.CountyModels", "Id", c => c.Int(nullable: false, identity: true));
            AlterColumn("dbo.AuctionInCountyModels", "CountyId", c => c.Int(nullable: false));
            AlterColumn("dbo.CountyModels", "CountyName", c => c.String(nullable: false));
            AddPrimaryKey("dbo.AuctionInCountyModels", new[] { "CountyId", "AuctionId" });
            AddPrimaryKey("dbo.CountyModels", "Id");
            CreateIndex("dbo.AuctionInCountyModels", "CountyId");
            AddForeignKey("dbo.AuctionInCountyModels", "CountyId", "dbo.CountyModels", "Id", cascadeDelete: true);
            DropColumn("dbo.AuctionInCountyModels", "CountyName");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AuctionInCountyModels", "CountyName", c => c.String(nullable: false, maxLength: 128));
            DropForeignKey("dbo.AuctionInCountyModels", "CountyId", "dbo.CountyModels");
            DropIndex("dbo.AuctionInCountyModels", new[] { "CountyId" });
            DropPrimaryKey("dbo.CountyModels");
            DropPrimaryKey("dbo.AuctionInCountyModels");
            AlterColumn("dbo.CountyModels", "CountyName", c => c.String(nullable: false, maxLength: 128));
            AlterColumn("dbo.AuctionInCountyModels", "CountyId", c => c.String(nullable: false, maxLength: 128));
            DropColumn("dbo.CountyModels", "Id");
            AddPrimaryKey("dbo.CountyModels", new[] { "CountyName", "StateName" });
            AddPrimaryKey("dbo.AuctionInCountyModels", new[] { "StateName", "CountyName", "AuctionId" });
            RenameColumn(table: "dbo.AuctionInCountyModels", name: "CountyId", newName: "StateName");
            CreateIndex("dbo.AuctionInCountyModels", new[] { "StateName", "CountyName" });
            AddForeignKey("dbo.AuctionInCountyModels", new[] { "StateName", "CountyName" }, "dbo.CountyModels", new[] { "CountyName", "StateName" }, cascadeDelete: true);
        }
    }
}
