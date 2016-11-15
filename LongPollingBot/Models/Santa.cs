using System;
using System.Collections.Generic;

namespace LongPollingBot.Models
{
    public class Santa
    {
        public Santa()
        {
            RegistrationDate = DateTime.UtcNow;
        }

        public int Id { get; set; }
        public long ChatId { get; set; }
        public string Username { get; set; }
        public Status Status { get; set; }
        public string Address { get; set; }
        public DateTime RegistrationDate { get; set; }
        public virtual List<Gift> Gifts { get; set; }
        public virtual List<Gift> GiftsToMe { get; set; }
    }
}