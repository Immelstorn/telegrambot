using System.Collections.Generic;

namespace LongPollingBot.Models
{
    public class Reciever
    {
        public int Id { get; set; }
        public virtual Room Room { get; set; }
        public virtual Santa WhoAmI { get; set; }

        public virtual List<Santa> Santas { get; set; }
    }
}