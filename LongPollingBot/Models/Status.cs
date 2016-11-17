namespace LongPollingBot.Models
{
    public enum Status
    {
        WaitingForPassword = 1,
        WaitingForAddress = 2,
        Accepted = 3,
        Quitting = 5
    }
}