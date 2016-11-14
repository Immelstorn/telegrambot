using System;
using System.Data.SqlTypes;

namespace LongPollingBot.Models
{
    public class Gift
    {
        public Gift()
        {
            CreationTime = DateTime.UtcNow;
            RecievedDate = SentDate = SqlDateTime.MinValue.Value;
        }
        public int Id { get; set; }
        public DateTime CreationTime { get; set; }
        public bool Sent { get; set; }
        public DateTime SentDate { get; set; }
        public bool Recieved { get; set; }
        public DateTime RecievedDate { get; set; }
        public string MessageFromSanta { get; set; }

        public virtual Santa Santa { get; set; }
        public virtual Room Room { get; set; }
        public virtual Santa Reciever { get; set; }
    }
}