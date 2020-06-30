namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DocumentContentType : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AccountDocumentsModels", "ContentType", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AccountDocumentsModels", "ContentType");
        }
    }
}
