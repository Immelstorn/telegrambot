namespace LongPollingBot.Models
{
    public enum Status
    {
        WaitingForLanguage = 0,
        WaitingForPassword = 1,
        WaitingForAddress = 2,
        Accepted = 3,
        Quitting = 5
    }
}