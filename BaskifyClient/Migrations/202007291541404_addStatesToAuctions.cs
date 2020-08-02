namespace BaskifyClient.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addStatesToAuctions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AuctionInStateModels",
                c => new
                    {
                        StateAbbrv = c.String(nullable: false, maxLength: 128),
                        AuctionId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.StateAbbrv, t.AuctionId })
                .ForeignKey("dbo.AuctionModels", t => t.AuctionId, cascadeDelete: true)
                .ForeignKey("dbo.StateModels", t => t.StateAbbrv, cascadeDelete: true)
                .Index(t => t.StateAbbrv)
                .Index(t => t.AuctionId);
            
            CreateTable(
                "dbo.StateModels",
                c => new
                    {
                        Abbrv = c.String(nullable: false, maxLength: 128),
                        FullName = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Abbrv);
            Sql("INSERT INTO dbo.StateModels (FullName, Abbrv) VALUES ('Alabama', 'AL'), ('Alaska', 'AK'), ('Arizona', 'AZ'), ('Arkansas', 'AR'), ('California', 'CA'), ('Colorado', 'CO'), ('Connecticut', 'CT'), ('Delaware', 'DE'), ('Florida', 'FL'), ('Georgia', 'GA'), ('Hawaii', 'HI'), ('Idaho', 'ID'), ('Illinois', 'IL'), ('Indiana', 'IN'), ('Iowa', 'IA'), ('Kansas', 'KS'), ('Kentucky', 'KY'), ('Louisiana', 'LA'), ('Maine', 'ME'), ('Maryland', 'MD'), ('Massachusetts', 'MA'), ('Michigan', 'MI'), ('Minnesota', 'MN'), ('Mississippi', 'MS'), ('Missouri', 'MO'), ('Montana', 'MT'), ('Nebraska', 'NE'), ('Nevada', 'NV'), ('New Hampshire', 'NH'), ('New Jersey', 'NJ'), ('New Mexico', 'NM'), ('New York', 'NY'), ('North Carolina', 'NC'), ('North Dakota', 'ND'), ('Ohio', 'OH'), ('Oklahoma', 'OK'), ('Oregon', 'OR'), ('Pennsylvania', 'PA'), ('Rhode Island', 'RI'), ('South Carolina', 'SC'), ('South Dakota', 'SD'), ('Tennessee', 'TN'), ('Texas', 'TX'), ('Utah', 'UT'), ('Vermont', 'VT'), ('Virginia', 'VA'), ('Washington', 'WA'), ('West Virginia', 'WV'), ('Wisconsin', 'WI'), ('Wyoming', 'WY'), ('American Samoa', 'AS'), ('District of Columbia', 'DC'), ('Federated States of Micronesia', 'FM'), ('Guam', 'GU'), ('Marshall Islands', 'MH'), ('Northern Mariana Islands', 'MP'), ('Palau', 'PW'), ('Puerto Rico', 'PR'), ('Virgin Islands', 'VI')");
        }

        public override void Down()
        {
            DropForeignKey("dbo.AuctionInStateModels", "StateAbbrv", "dbo.StateModels");
            DropForeignKey("dbo.AuctionInStateModels", "AuctionId", "dbo.AuctionModels");
            DropIndex("dbo.AuctionInStateModels", new[] { "AuctionId" });
            DropIndex("dbo.AuctionInStateModels", new[] { "StateAbbrv" });
            DropTable("dbo.StateModels");
            DropTable("dbo.AuctionInStateModels");
        }
    }
}
