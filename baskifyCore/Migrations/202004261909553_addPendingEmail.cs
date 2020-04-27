namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addPendingEmail : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserModels", "PendingEmail", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "PendingEmail");
        }
    }
}
