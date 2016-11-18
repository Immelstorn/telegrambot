using System;
using System.Collections.Generic;

namespace LongPollingBot.Models
{
    public class Room
    {
        public Room()
        {
            TimeToSend = DateTime.UtcNow < new DateTime(2016, 12, 04) ? new DateTime(2016, 12, 04) : new DateTime(2017, 12, 04);
        }
        public int Id { get; set; }
        public string Password { get; set; }
        public virtual List<Gift> Gifts { get; set; }
        public DateTime TimeToSend { get; set; }
        public bool MessagesSent { get; set; }
        public bool ReminderSent { get; set; }
        public virtual Santa Creator { get; set; }
    }
}