namespace TelegramBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class multipleRecievers : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Santas", "Reciever_Id", "dbo.Santas");
            DropIndex("dbo.Santas", new[] { "Reciever_Id" });
            CreateTable(
                "dbo.Recievers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Room_Id = c.Int(),
                        WhoAmI_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Rooms", t => t.Room_Id)
                .ForeignKey("dbo.Santas", t => t.WhoAmI_Id)
                .Index(t => t.Room_Id)
                .Index(t => t.WhoAmI_Id);
            
            CreateTable(
                "dbo.SantaRecievers",
                c => new
                    {
                        Santa_Id = c.Int(nullable: false),
                        Reciever_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Santa_Id, t.Reciever_Id })
                .ForeignKey("dbo.Santas", t => t.Santa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Recievers", t => t.Reciever_Id, cascadeDelete: true)
                .Index(t => t.Santa_Id)
                .Index(t => t.Reciever_Id);
            
            DropColumn("dbo.Santas", "Reciever_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Santas", "Reciever_Id", c => c.Int());
            DropForeignKey("dbo.SantaRecievers", "Reciever_Id", "dbo.Recievers");
            DropForeignKey("dbo.SantaRecievers", "Santa_Id", "dbo.Santas");
            DropForeignKey("dbo.Recievers", "WhoAmI_Id", "dbo.Santas");
            DropForeignKey("dbo.Recievers", "Room_Id", "dbo.Rooms");
            DropIndex("dbo.SantaRecievers", new[] { "Reciever_Id" });
            DropIndex("dbo.SantaRecievers", new[] { "Santa_Id" });
            DropIndex("dbo.Recievers", new[] { "WhoAmI_Id" });
            DropIndex("dbo.Recievers", new[] { "Room_Id" });
            DropTable("dbo.SantaRecievers");
            DropTable("dbo.Recievers");
            CreateIndex("dbo.Santas", "Reciever_Id");
            AddForeignKey("dbo.Santas", "Reciever_Id", "dbo.Santas", "Id");
        }
    }
}
