using System;
using System.Configuration;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web.Http;

using Telegram.Bot;
using Telegram.Bot.Types;

using TelegramBot.Models;

namespace TelegramBot.Controllers
{
    public class SecretSantaController : ApiController
    {
        private readonly TelegramBotClient _bot = new TelegramBotClient(ConfigurationManager.AppSettings["Token"]);

        public string Get()
        {
            return "Ok!";
        }

        public async Task Post(Update update)
        {
            using(var db = new SecretSantaDbContext())
            {
                if(DateTime.Now < new DateTime(2016, 12, 04))
                {
                    var santa = await db.Santas.FirstOrDefaultAsync(s => s.Username == update.Message.From.Username);
                    if(santa == null)
                    {
                        var newSanta = new Santa {
                            Username = update.Message.From.Username,
                            Status = Status.WaitingForPassword
                        };

                        db.Santas.Add(newSanta);
                        await db.SaveChangesAsync();
                        await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Судя по всему ты тут новенький. Введи пароль к существующей комнате или новый пароль для создания новой комнаты.");
                        return;
                    }

                    if(santa.Status == Status.WaitingForPassword)
                    {
                        var password = update.Message.Text;
                        if(string.IsNullOrEmpty(password) || password.Length < 6)
                        {
                            await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.");
                            return;
                        }

                        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Password.Equals(password));
                        if(room == null)
                        {
                            var newRoom = new Room {
                                Password = password
                            };

                            db.Rooms.Add(newRoom);
                            santa.Room = newRoom;
                            santa.Status = Status.WaitingForAddress;
                            await db.SaveChangesAsync();
                            await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Новая комната создана. Приглашай друзей с помощью пароля для комнаты.");
                            await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой адрес, на который твой Санта вышлет тебе подарок.");
                            return;
                        }

                        santa.Room = room;
                        santa.Status = Status.WaitingForAddress;
                        await db.SaveChangesAsync();
                        var count = await db.Santas.CountAsync(s => s.Room == room);
                        await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты добавлен в эту комнату, сейчас тут {count} человек.");
                        await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой адрес, на который твой Санта вышлет тебе подарок.");
                        return;
                    }

                    if(santa.Status == Status.WaitingForAddress)
                    {
                        var address = update.Message.Text;
                        if(string.IsNullOrEmpty(address) || address.Length < 10)
                        {
                            await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Не бывает таких коротких адресов. Помни что тебе также надо указать свои ФИО или что там требует почта в твоей стране.");
                            return;
                        }
                        santa.Address = address;
                        santa.Status = Status.Accepted;
                        await db.SaveChangesAsync();
                        await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Отлично! Адрес сохранен. 4 декабря я всех перемешаю и пришлю тебе адрес другого человека которому ты должен будешь отправить подарок.");
                        return;
                    }

                    if(santa.Status == Status.Accepted || santa.Status == Status.ChangeAddress || santa.Status == Status.Quitting)
                    {
                        switch(update.Message.Text)
                        {
                            case "/help":
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"/help - помощь \n/change - сменить адрес \n/info - посмотреть свой адрес и комнату \n/count - узнать количество человек в комнате \n/quit - выйти из игры");
                                return;

                            case "/change":
                                santa.Status = Status.ChangeAddress;
                                await db.SaveChangesAsync();
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой новый адрес, на который твой Санта вышлет тебе подарок.");
                                return;

                            case "/count":
                                var roomId = santa.Room.Id;
                                var count = await db.Santas.CountAsync(s => s.Room.Id == roomId);
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Cейчас тут {count} человек.");
                                return;

                            case "/info":
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Твой текущий адрес - {santa.Address}, твоя комната - {santa.Room.Password}");
                                return;

                            case "/quit":
                                santa.Status = Status.Quitting;
                                await db.SaveChangesAsync();
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Уверен? Для подтверждения напиши \"Да\"");
                                return;

                            default:
                                if(santa.Status == Status.ChangeAddress)
                                {
                                    var address = update.Message.Text;
                                    if (string.IsNullOrEmpty(address) || address.Length < 10)
                                    {
                                        await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Не бывает таких коротких адресов. Помни что тебе также надо указать свои ФИО или что там требует почта в твоей стране.");
                                        return;
                                    }
                                    santa.Address = address;
                                    santa.Status = Status.Accepted;
                                    await db.SaveChangesAsync();
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Отлично! Адрес сохранен. 4 декабря я всех перемешаю и пришлю тебе адрес другого человека которому ты должен будешь отправить подарок.");
                                    return;
                                }

                                if(santa.Status == Status.Quitting)
                                {
                                    if(update.Message.Text.Equals("да", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        db.Santas.Remove(santa);
                                        await db.SaveChangesAsync();
                                        await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Очень жаль. Передумаешь - возвращайся.");
                                        return;
                                    }
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Буду считать что ты передумал удаляться");
                                    return;
                                }

                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Извини, я не понимаю что ты хочешь сделать, попробуй воспользоваться помощью - /help");
                                return;
                        }
                    }
                }
            }
        }
    }
}
