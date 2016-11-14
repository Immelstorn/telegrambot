namespace LongPollingBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddGifts : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Recievers", "Room_Id", "dbo.Rooms");
            DropForeignKey("dbo.Recievers", "WhoAmI_Id", "dbo.Santas");
            DropForeignKey("dbo.SantaRecievers", "Santa_Id", "dbo.Santas");
            DropForeignKey("dbo.SantaRecievers", "Reciever_Id", "dbo.Recievers");
            DropForeignKey("dbo.SantaRooms", "Santa_Id", "dbo.Santas");
            DropForeignKey("dbo.SantaRooms", "Room_Id", "dbo.Rooms");
            DropIndex("dbo.Recievers", new[] { "Room_Id" });
            DropIndex("dbo.Recievers", new[] { "WhoAmI_Id" });
            DropIndex("dbo.SantaRecievers", new[] { "Santa_Id" });
            DropIndex("dbo.SantaRecievers", new[] { "Reciever_Id" });
            DropIndex("dbo.SantaRooms", new[] { "Santa_Id" });
            DropIndex("dbo.SantaRooms", new[] { "Room_Id" });
            CreateTable(
                "dbo.Gifts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CreationTime = c.DateTime(nullable: false),
                        Sent = c.Boolean(nullable: false),
                        SentDate = c.DateTime(nullable: false),
                        Recieved = c.Boolean(nullable: false),
                        RecievedDate = c.DateTime(nullable: false),
                        MessageFromSanta = c.String(),
                        Santa_Id = c.Int(nullable: false),
                        Reciever_Id = c.Int(),
                        Room_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Santas", t => t.Santa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Santas", t => t.Reciever_Id)
                .ForeignKey("dbo.Rooms", t => t.Room_Id, cascadeDelete: true)
                .Index(t => t.Santa_Id)
                .Index(t => t.Reciever_Id)
                .Index(t => t.Room_Id);
            
            AddColumn("dbo.Santas", "RegistrationDate", c => c.DateTime(nullable: false));
            DropTable("dbo.Recievers");
            DropTable("dbo.SantaRecievers");
            DropTable("dbo.SantaRooms");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.SantaRooms",
                c => new
                    {
                        Santa_Id = c.Int(nullable: false),
                        Room_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Santa_Id, t.Room_Id });
            
            CreateTable(
                "dbo.SantaRecievers",
                c => new
                    {
                        Santa_Id = c.Int(nullable: false),
                        Reciever_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Santa_Id, t.Reciever_Id });
            
            CreateTable(
                "dbo.Recievers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Room_Id = c.Int(),
                        WhoAmI_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
            
            DropForeignKey("dbo.Gifts", "Room_Id", "dbo.Rooms");
            DropForeignKey("dbo.Gifts", "Reciever_Id", "dbo.Santas");
            DropForeignKey("dbo.Gifts", "Santa_Id", "dbo.Santas");
            DropIndex("dbo.Gifts", new[] { "Room_Id" });
            DropIndex("dbo.Gifts", new[] { "Reciever_Id" });
            DropIndex("dbo.Gifts", new[] { "Santa_Id" });
            DropColumn("dbo.Santas", "RegistrationDate");
            DropTable("dbo.Gifts");
            CreateIndex("dbo.SantaRooms", "Room_Id");
            CreateIndex("dbo.SantaRooms", "Santa_Id");
            CreateIndex("dbo.SantaRecievers", "Reciever_Id");
            CreateIndex("dbo.SantaRecievers", "Santa_Id");
            CreateIndex("dbo.Recievers", "WhoAmI_Id");
            CreateIndex("dbo.Recievers", "Room_Id");
            AddForeignKey("dbo.SantaRooms", "Room_Id", "dbo.Rooms", "Id", cascadeDelete: true);
            AddForeignKey("dbo.SantaRooms", "Santa_Id", "dbo.Santas", "Id", cascadeDelete: true);
            AddForeignKey("dbo.SantaRecievers", "Reciever_Id", "dbo.Recievers", "Id", cascadeDelete: true);
            AddForeignKey("dbo.SantaRecievers", "Santa_Id", "dbo.Santas", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Recievers", "WhoAmI_Id", "dbo.Santas", "Id");
            AddForeignKey("dbo.Recievers", "Room_Id", "dbo.Rooms", "Id");
        }
    }
}
