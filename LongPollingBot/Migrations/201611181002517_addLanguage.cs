namespace LongPollingBot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addLanguage : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Santas", "Language", c => c.Int(nullable: false, defaultValue: 0));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Santas", "Language");
        }
    }
}
