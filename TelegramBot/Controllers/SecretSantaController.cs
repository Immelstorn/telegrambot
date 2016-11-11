using System;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
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
            Debug.Write("Debug");
            Trace.TraceInformation("TraceInformation");
            Trace.TraceError("TraceError");
            Console.WriteLine("console");
            return "Ok!";
        }

        public async Task Post(Update update)
        {
            Trace.TraceInformation(update.Message.Text);
            Trace.TraceError(update.Message.Text);
            try
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

                        if (santa.Status == Status.WaitingForPassword)
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
                                santa.Rooms.Add(newRoom);
                                santa.Status = Status.WaitingForAddress;
                                await db.SaveChangesAsync();
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Новая комната создана. Приглашай друзей с помощью пароля для комнаты.");
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой адрес, на который твой Санта вышлет тебе подарок.");
                                return;
                            }

                            var count = await db.Santas.CountAsync(s => s.Rooms.Any(r => r.Id == room.Id));
                            if(santa.Rooms.Any(r => r.Id == room.Id))
                            {
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты уже добавлен в эту комнату, сейчас тут {count} человек.");
                                return;
                            }

                            santa.Rooms.Add(room);
                            santa.Status = Status.WaitingForAddress;
                            await db.SaveChangesAsync();
                            await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты добавлен в эту комнату, сейчас тут {count} человек.");
                            await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой адрес, на который твой Санта вышлет тебе подарок.");
                            return;
                        }

                        if (santa.Status == Status.WaitingForAddress)
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

                        if (santa.Status == Status.Accepted || santa.Status == Status.ChangeAddress)
                        {
                            if(update.Message.Text.StartsWith("/help"))
                            {
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"/help - помощь \n/change - сменить адрес \n/addroom <пароль к комнате> - добавить комнату \n/info - посмотреть свой адрес и комнаты \n/count <пароль к комнате> - узнать количество человек в комнате \n/quit <пароль к комнате> - выйти из игры");
                                return;
                            }
                            else if(update.Message.Text.StartsWith("/change"))
                            {
                                santa.Status = Status.ChangeAddress;
                                await db.SaveChangesAsync();
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой новый адрес, на который твой Санта вышлет тебе подарок.");
                                return;
                            }
                            else if(update.Message.Text.StartsWith("/addroom"))
                            {
                                var password = update.Message.Text.Replace("/addroom ", string.Empty);
                                if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/addroom") || password.Length < 6)
                                {
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.");
                                    return;
                                }

                                var room = await db.Rooms.FirstOrDefaultAsync(r => r.Password.Equals(password));
                                if (room == null)
                                {
                                    var newRoom = new Room
                                    {
                                        Password = password
                                    };

                                    db.Rooms.Add(newRoom);
                                    santa.Rooms.Add(newRoom);
                                    await db.SaveChangesAsync();
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Новая комната создана. Приглашай друзей с помощью пароля для комнаты.");
                                    return;
                                }

                                var count = await db.Santas.CountAsync(s => s.Rooms.Any(r => r.Id == room.Id));
                                if (santa.Rooms.Any(r => r.Id == room.Id))
                                {
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты уже добавлен в эту комнату, сейчас тут {count} человек.");
                                    return;
                                }

                                santa.Rooms.Add(room);
                                await db.SaveChangesAsync();
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты добавлен в эту комнату, сейчас тут {count} человек.");
                                return;
                            }
                            else if(update.Message.Text.StartsWith("/count"))
                            {
                                var password = update.Message.Text.Replace("/count ", string.Empty);
                                if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/count") || password.Length < 6)
                                {
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.");
                                    return;
                                }
                                var room = await db.Rooms.FirstOrDefaultAsync(r => r.Password.Equals(password));
                                if(room == null)
                                {
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует, ты можешь добавить ее с помощью комманды /addroom <пароль к комнате>");
                                    return;
                                }

                                var roomId = room.Id;
                                var count = await db.Santas.CountAsync(s => s.Rooms.Any(r => r.Id == roomId));

                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Cейчас тут {count} человек.");
                            }
                            else if(update.Message.Text.StartsWith("/info"))
                            {
                                var rooms = santa.Rooms.Aggregate(string.Empty, (current, room) => current + $" {room.Password},");
                                rooms = rooms.Substring(1, rooms.Length - 2);
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Твой текущий адрес - {santa.Address}, твои комнаты: {rooms}");
                            }
                            else if(update.Message.Text.StartsWith("/quit"))
                            {
                                var password = update.Message.Text.Replace("/quit ", string.Empty);
                                if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/quit") || password.Length < 6)
                                {
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.");
                                    return;
                                }
                                var room = await db.Rooms.FirstOrDefaultAsync(r => r.Password.Equals(password));
                                if (room == null || santa.Rooms.All(r => r.Id != room.Id))
                                {
                                    await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует");
                                    return;
                                }
                                santa.Rooms.Remove(room);
                                await db.SaveChangesAsync();
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Очень жаль. Передумаешь - возвращайся.");
                                return;
                            }
                            else
                            {
                                if(santa.Status == Status.ChangeAddress)
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
                                await _bot.SendTextMessageAsync(update.Message.Chat.Id, "Извини, я не понимаю что ты хочешь сделать, попробуй воспользоваться помощью - /help");
                                return;
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Trace.TraceError(e.Message);
            }
        }
    }
}
