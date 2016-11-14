using System.Data.Entity;
namespace LongPollingBot.Models
{
    public class SecretSantaDbContext: DbContext
    {
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Santa> Santas { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<Gift> Gifts { get; set; }

        public SecretSantaDbContext(): base("name=SecretSantaConnectionString") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Santa>().HasMany(s => s.Gifts).WithRequired(r => r.Santa);
            modelBuilder.Entity<Santa>().HasMany(s => s.GiftsToMe);

            modelBuilder.Entity<Gift>().HasRequired(g => g.Room).WithMany(r => r.Gifts);
            modelBuilder.Entity<Gift>().HasOptional(g => g.Reciever).WithMany(r => r.GiftsToMe);
        }
    }
}