using System.Collections.Generic;

namespace LongPollingBot.Models
{
    public class Santa
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public Status Status { get; set; }
        public string Address { get; set; }

        public virtual List<Room> Rooms { get; set; }
        public virtual List<Reciever> Recievers { get; set; }


    }
}