using System.Data.Entity;

namespace TelegramBot.Models
{
    public class SecretSantaDbContext: DbContext
    {
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Santa> Santas { get; set; }

        public SecretSantaDbContext(): base("name=SecretSantaConnectionString") { }
    }
}