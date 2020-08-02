namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class contactPageModelReqs : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ContactModels", "Email", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ContactModels", "Email", c => c.String());
        }
    }
}
