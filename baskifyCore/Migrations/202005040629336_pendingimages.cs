namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class pendingimages : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PendingImageModels",
                c => new
                    {
                        ImageUrl = c.String(nullable: false, maxLength: 128),
                        UploadTime = c.DateTime(nullable: false),
                        Username = c.String(maxLength: 30),
                    })
                .PrimaryKey(t => t.ImageUrl)
                .ForeignKey("dbo.UserModels", t => t.Username)
                .Index(t => t.Username);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PendingImageModels", "Username", "dbo.UserModels");
            DropIndex("dbo.PendingImageModels", new[] { "Username" });
            DropTable("dbo.PendingImageModels");
        }
    }
}
