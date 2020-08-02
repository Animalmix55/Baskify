namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class StripeRegistration : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.StripeRegistrationModels",
                c => new
                    {
                        State = c.Guid(nullable: false),
                        Username = c.String(nullable: false, maxLength: 30),
                        TimeCreated = c.DateTime(nullable: false),
                        Complete = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.State)
                .ForeignKey("dbo.UserModels", t => t.Username, cascadeDelete: true)
                .Index(t => t.Username);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StripeRegistrationModels", "Username", "dbo.UserModels");
            DropIndex("dbo.StripeRegistrationModels", new[] { "Username" });
            DropTable("dbo.StripeRegistrationModels");
        }
    }
}
