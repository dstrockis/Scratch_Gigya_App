namespace MyGigyaSite.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AppUsers",
                c => new
                    {
                        AppUserID = c.String(nullable: false, maxLength: 128),
                        Email = c.String(nullable: false),
                        Password = c.String(),
                    })
                .PrimaryKey(t => t.AppUserID);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.AppUsers");
        }
    }
}
