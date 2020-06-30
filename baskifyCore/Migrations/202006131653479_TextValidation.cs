namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TextValidation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.VerificationCodeModels",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        Payload = c.String(),
                        Secret = c.String(),
                        TimeCreated = c.DateTime(nullable: false),
                        VerificationType = c.Int(nullable: false),
                        Validated = c.Boolean(nullable: false),
                        ValidatedOn = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.UserModels", "PhoneNumber", c => c.String());
            AddColumn("dbo.UserModels", "isMFA", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "isMFA");
            DropColumn("dbo.UserModels", "PhoneNumber");
            DropTable("dbo.VerificationCodeModels");
        }
    }
}
