namespace TelegramBot.Models
{
    public enum Status
    {
        WaitingForPassword = 1,
        WaitingForAddress = 2,
        Accepted = 3,
        ChangeAddress = 4,
        Quitting = 5
    }
}