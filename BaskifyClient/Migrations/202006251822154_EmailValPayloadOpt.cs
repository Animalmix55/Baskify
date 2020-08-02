namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EmailValPayloadOpt : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.EmailVerificationModels", "Payload", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.EmailVerificationModels", "Payload", c => c.String(nullable: false));
        }
    }
}
