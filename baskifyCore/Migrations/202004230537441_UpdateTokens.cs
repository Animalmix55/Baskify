namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateTokens : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserModels", "Email", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserModels", "Email");
        }
    }
}
