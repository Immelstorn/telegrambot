namespace TelegramBot.Models
{
    public class Santa
    {
        public int Id { get; set; }
        public virtual Room Room { get; set; }
        public string Username { get; set; }
        public virtual Santa Reciever { get; set; }
        public Status Status { get; set; }

        public string Address { get; set; }
    }
}