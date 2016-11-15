namespace LongPollingBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class reminderSent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Rooms", "ReminderSent", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Rooms", "ReminderSent");
        }
    }
}
