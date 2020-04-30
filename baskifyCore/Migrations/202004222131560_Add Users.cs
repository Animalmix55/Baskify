namespace baskifyCore.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUsers : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserModels",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        FirstName = c.String(nullable: true),
                        LastName = c.String(nullable: true),
                        Address = c.String(),
                        City = c.String(),
                        State = c.String(),
                        ZIP = c.String(),
                        UserRole = c.Int(nullable: false),
                        DateOfBirth = c.DateTime(nullable: true),
                        iconUrl = c.String(nullable: false, defaultValue: "/Content/unknownUser.png"),
                        lastLogin = c.DateTime(nullable: true),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.UserModels");
        }
    }
}
