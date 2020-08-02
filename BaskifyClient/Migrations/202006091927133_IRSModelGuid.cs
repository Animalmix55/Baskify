namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class IRSModelGuid : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.IRSNonProfitIndexModels");
            AddColumn("dbo.IRSNonProfitIndexModels", "Id", c => c.Guid(nullable: false, identity: true));
            AlterColumn("dbo.IRSNonProfitIndexModels", "DLN", c => c.String(nullable: false));
            AddPrimaryKey("dbo.IRSNonProfitIndexModels", "Id");
        }
        
        public override void Down()
        {
            DropPrimaryKey("dbo.IRSNonProfitIndexModels");
            AlterColumn("dbo.IRSNonProfitIndexModels", "DLN", c => c.String(nullable: false, maxLength: 128));
            DropColumn("dbo.IRSNonProfitIndexModels", "Id");
            AddPrimaryKey("dbo.IRSNonProfitIndexModels", "DLN");
        }
    }
}
