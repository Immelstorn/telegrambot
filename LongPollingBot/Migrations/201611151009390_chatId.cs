namespace LongPollingBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class chatId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Santas", "ChatId", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Santas", "ChatId");
        }
    }
}
