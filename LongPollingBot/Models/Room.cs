using System.Collections.Generic;

namespace LongPollingBot.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Password { get; set; }

        public virtual List<Santa> Santas { get; set; }

    }
}