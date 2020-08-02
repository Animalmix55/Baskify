namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class contactPageModelChange : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ContactModels", "Subject", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ContactModels", "Subject");
        }
    }
}
