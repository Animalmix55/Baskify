namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ValidationGeneratedId : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.VerificationCodeModels");
            AlterColumn("dbo.VerificationCodeModels", "Id", c => c.Guid(nullable: false, identity: true));
            AlterColumn("dbo.VerificationCodeModels", "Secret", c => c.String(nullable: false));
            AddPrimaryKey("dbo.VerificationCodeModels", "Id");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.VerificationCodeModels");
            AlterColumn("dbo.VerificationCodeModels", "Secret", c => c.String());
            AlterColumn("dbo.VerificationCodeModels", "Id", c => c.Guid(nullable: false));
            AddPrimaryKey("dbo.VerificationCodeModels", "Id");
        }
    }
}
