using System.Data.Entity.Migrations;

namespace LongPollingBot.Migrations
{

    public partial class multiplerooms : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Santas", "Room_Id", "dbo.Rooms");
            DropIndex("dbo.Santas", new[] { "Room_Id" });
            CreateTable(
                "dbo.SantaRooms",
                c => new
                    {
                        Santa_Id = c.Int(nullable: false),
                        Room_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Santa_Id, t.Room_Id })
                .ForeignKey("dbo.Santas", t => t.Santa_Id, cascadeDelete: true)
                .ForeignKey("dbo.Rooms", t => t.Room_Id, cascadeDelete: true)
                .Index(t => t.Santa_Id)
                .Index(t => t.Room_Id);
            
            DropColumn("dbo.Santas", "Room_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Santas", "Room_Id", c => c.Int());
            DropForeignKey("dbo.SantaRooms", "Room_Id", "dbo.Rooms");
            DropForeignKey("dbo.SantaRooms", "Santa_Id", "dbo.Santas");
            DropIndex("dbo.SantaRooms", new[] { "Room_Id" });
            DropIndex("dbo.SantaRooms", new[] { "Santa_Id" });
            DropTable("dbo.SantaRooms");
            CreateIndex("dbo.Santas", "Room_Id");
            AddForeignKey("dbo.Santas", "Room_Id", "dbo.Rooms", "Id");
        }
    }
}
