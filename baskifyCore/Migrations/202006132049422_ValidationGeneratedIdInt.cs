namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ValidationGeneratedIdInt : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.VerificationCodeModels");
            DropColumn("dbo.VerificationCodeModels", "Id");
            AddColumn("dbo.VerificationCodeModels", "Id", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.VerificationCodeModels", "Id");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.VerificationCodeModels");
            DropColumn("dbo.VerificationCodeModels", "Id");
            AddColumn("dbo.VerificationCodeModels", "Id", c => c.Guid(nullable: false, identity: true));
            AddPrimaryKey("dbo.VerificationCodeModels", "Id");
        }
    }
}
