using System;

using Telegram.Bot;

namespace WebhookRegister
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 0)
            {
                var key = args[0];
                var hook = args.Length > 1 ? args[1] : string.Empty;
                var client = new TelegramBotClient(key);
                client.SetWebhookAsync(hook).Wait();
                Console.WriteLine();
                Console.WriteLine($"Token: {key}");
                Console.WriteLine($"Hook: {hook}");
                Console.WriteLine("Done");
            }
        }
    }
}
