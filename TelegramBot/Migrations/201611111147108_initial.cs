namespace TelegramBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Rooms",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Password = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Santas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Username = c.String(),
                        Status = c.Int(nullable: false),
                        Address = c.String(),
                        Reciever_Id = c.Int(),
                        Room_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Santas", t => t.Reciever_Id)
                .ForeignKey("dbo.Rooms", t => t.Room_Id)
                .Index(t => t.Reciever_Id)
                .Index(t => t.Room_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Santas", "Room_Id", "dbo.Rooms");
            DropForeignKey("dbo.Santas", "Reciever_Id", "dbo.Santas");
            DropIndex("dbo.Santas", new[] { "Room_Id" });
            DropIndex("dbo.Santas", new[] { "Reciever_Id" });
            DropTable("dbo.Santas");
            DropTable("dbo.Rooms");
        }
    }
}
