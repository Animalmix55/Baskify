namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemoveRollBack : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.EmailVerificationModels", "Reverted");
        }
        
        public override void Down()
        {
            AddColumn("dbo.EmailVerificationModels", "Reverted", c => c.Boolean(nullable: false));
        }
    }
}
