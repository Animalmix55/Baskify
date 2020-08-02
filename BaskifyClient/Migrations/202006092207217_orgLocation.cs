namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class orgLocation : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.IRSNonProfits", "City", c => c.String(nullable: false, maxLength: 60));
            AlterColumn("dbo.IRSNonProfits", "State", c => c.String(nullable: false, maxLength: 2));
            AlterColumn("dbo.IRSNonProfits", "Country", c => c.String(nullable: false, maxLength: 100));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.IRSNonProfits", "Country", c => c.String(maxLength: 100));
            AlterColumn("dbo.IRSNonProfits", "State", c => c.String(maxLength: 2));
            AlterColumn("dbo.IRSNonProfits", "City", c => c.String(maxLength: 60));
        }
    }
}
