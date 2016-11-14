namespace LongPollingBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class timeToSend : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Rooms", "TimeToSend", c => c.DateTime(nullable: false, defaultValue: new DateTime(2016, 12, 4)));
            AddColumn("dbo.Rooms", "MessagesSent", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Rooms", "MessagesSent");
            DropColumn("dbo.Rooms", "TimeToSend");
        }
    }
}
