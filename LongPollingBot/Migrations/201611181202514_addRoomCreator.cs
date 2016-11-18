namespace LongPollingBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addRoomCreator : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Rooms", "Creator_Id", c => c.Int());
            CreateIndex("dbo.Rooms", "Creator_Id");
            AddForeignKey("dbo.Rooms", "Creator_Id", "dbo.Santas", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Rooms", "Creator_Id", "dbo.Santas");
            DropIndex("dbo.Rooms", new[] { "Creator_Id" });
            DropColumn("dbo.Rooms", "Creator_Id");
        }
    }
}
