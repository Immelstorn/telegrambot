using System;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

using LongPollingBot.Models;

using Quartz;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace LongPollingBot
{
    class TelegramJob : IJob
    {
        private readonly TelegramBotClient _bot = new TelegramBotClient(ConfigurationManager.AppSettings["Token"]);

        public void Execute(IJobExecutionContext context)
        {
            using(var db = new SecretSantaDbContext())
            {
                var setting = db.Settings.FirstOrDefault();
                var offset = 345894586;
                if(setting == null)
                {
                    db.Settings.Add(new Settings {
                        Offset = 345894586
                    });
                    db.SaveChanges();
                }
                else
                {
                    offset = setting.Offset;
                }

                var updates = _bot.GetUpdatesAsync(offset).Result;
                Console.WriteLine(updates.Length);
                foreach (var update in updates)
                {
                    DoStuff(update);




                    db.Settings.First().Offset = update.Id+1;
                    db.SaveChanges();
                }
            }
        }

        private void DoStuff(Update update)
        {
            try
            {
                using (var db = new SecretSantaDbContext())
                {
                    if (DateTime.Now < new DateTime(2016, 12, 04))
                    {
                        var santa = db.Santas.FirstOrDefaultAsync(s => s.Username == update.Message.From.Username).Result;
                        if (santa == null)
                        {
                            var newSanta = new Santa
                            {
                                Username = update.Message.From.Username,
                                Status = Status.WaitingForPassword
                            };

                            db.Santas.Add(newSanta);
                            db.SaveChanges();
                            _bot.SendTextMessageAsync(update.Message.Chat.Id, "Судя по всему ты тут новенький.\n С принципиами Secret Santa ты, я надеюсь, знаком.\n Здесь люди объединяются по комнатам с помощью секретного пароля и собираются дарить друг другу подарки.\n").Wait();
                            _bot.SendTextMessageAsync(update.Message.Chat.Id, "Введи пароль к существующей комнате или новый пароль для создания новой комнаты.").Wait();
                            return;
                        }

                        if (santa.Status == Status.WaitingForPassword)
                        {
                            var password = update.Message.Text;
                            if (string.IsNullOrEmpty(password) || password.Length < 6)
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.").Wait();
                                return;
                            }

                            var room =  db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
                            if (room == null)
                            {
                                var newRoom = new Room
                                {
                                    Password = password
                                };

                                db.Rooms.Add(newRoom);
                                santa.Rooms.Add(newRoom);
                                santa.Status = Status.WaitingForAddress;
                                db.SaveChanges();
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Новая комната создана. Приглашай друзей с помощью пароля для комнаты.").Wait();
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой адрес, на который твой Санта вышлет тебе подарок.").Wait();
                                return;
                            }

                            var count = db.Santas.Count(s => s.Rooms.Any(r => r.Id == room.Id));
                            if (santa.Rooms.Any(r => r.Id == room.Id))
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты уже добавлен в эту комнату, сейчас тут {count} человек.").Wait();
                                return;
                            }

                            santa.Rooms.Add(room);
                            santa.Status = Status.WaitingForAddress;
                            db.SaveChanges();
                            _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты добавлен в эту комнату, сейчас тут {count} человек.").Wait();
                            _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой адрес, на который твой Санта вышлет тебе подарок.").Wait();
                            return;
                        }

                        if (santa.Status == Status.WaitingForAddress)
                        {
                            var address = update.Message.Text;
                            if (string.IsNullOrEmpty(address) || address.Length < 10)
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Не бывает таких коротких адресов. Помни что тебе также надо указать свои ФИО или что там требует почта в твоей стране.").Wait();
                                return;
                            }
                            santa.Address = address;
                            santa.Status = Status.Accepted;
                            db.SaveChanges();
                            _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Отлично! Адрес сохранен. 4 декабря я всех перемешаю и пришлю тебе адрес другого человека, которому ты должен будешь отправить подарок.").Wait();
                            return;
                        }

                        if (santa.Status == Status.Accepted || santa.Status == Status.ChangeAddress)
                        {
                            if (update.Message.Text.StartsWith("/help"))
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"/help - помощь \n/change - сменить адрес \n/addroom <пароль к комнате> - добавить комнату \n/info - посмотреть свой адрес и комнаты, в которых ты находишься \n/count <пароль к комнате> - узнать количество человек в комнате \n/quit <пароль к комнате> - выйти из игры").Wait();
                                return;
                            }
                            else if (update.Message.Text.StartsWith("/change"))
                            {
                                santa.Status = Status.ChangeAddress;
                                db.SaveChanges();
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой новый адрес, на который твой Санта вышлет тебе подарок.").Wait();
                                return;
                            }
                            else if (update.Message.Text.StartsWith("/addroom"))
                            {
                                var password = update.Message.Text.Replace("/addroom ", string.Empty);
                                if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/addroom") || password.Length < 6)
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.").Wait();
                                    return;
                                }

                                var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
                                if (room == null)
                                {
                                    var newRoom = new Room
                                    {
                                        Password = password
                                    };

                                    db.Rooms.Add(newRoom);
                                    santa.Rooms.Add(newRoom);
                                    db.SaveChanges();
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Новая комната создана. Приглашай друзей с помощью пароля для комнаты.").Wait();
                                    return;
                                }

                                var count = db.Santas.Count(s => s.Rooms.Any(r => r.Id == room.Id));
                                if (santa.Rooms.Any(r => r.Id == room.Id))
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты уже добавлен в эту комнату, сейчас тут {count} человек.").Wait();
                                    return;
                                }

                                santa.Rooms.Add(room);
                                db.SaveChanges();
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты добавлен в эту комнату, сейчас тут {count} человек.").Wait();
                                return;
                            }
                            else if (update.Message.Text.StartsWith("/count"))
                            {
                                var password = update.Message.Text.Replace("/count ", string.Empty);
                                if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/count") || password.Length < 6)
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.").Wait();
                                    return;
                                }
                                var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
                                if (room == null)
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует, ты можешь добавить ее с помощью комманды /addroom <пароль к комнате>").Wait();
                                    return;
                                }

                                var roomId = room.Id;
                                var count = db.Santas.Count(s => s.Rooms.Any(r => r.Id == roomId));

                                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Cейчас тут {count} человек.").Wait();
                                return;
                            }
                            else if (update.Message.Text.StartsWith("/info"))
                            {
                                var rooms = "";
                                if(santa.Rooms.Any())
                                {
                                    rooms = santa.Rooms.Aggregate(string.Empty, (current, room) => current + $" {room.Password},");
                                    rooms = rooms.Substring(1, rooms.Length - 2);
                                }
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Твой текущий адрес - {santa.Address}, твои комнаты: {rooms}").Wait();
                                return;
                            }
                            else if (update.Message.Text.StartsWith("/quit"))
                            {
                                var password = update.Message.Text.Replace("/quit ", string.Empty);
                                if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/quit") || password.Length < 6)
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 6 символов.").Wait();
                                    return;
                                }
                                var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
                                if (room == null || santa.Rooms.All(r => r.Id != room.Id))
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует").Wait();
                                    return;
                                }
                                santa.Rooms.Remove(room);
                                db.SaveChanges();
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Очень жаль. Передумаешь - возвращайся.").Wait();
                                return;
                            }
                            else
                            {
                                if (santa.Status == Status.ChangeAddress)
                                {
                                    var address = update.Message.Text;
                                    if (string.IsNullOrEmpty(address) || address.Length < 10)
                                    {
                                        _bot.SendTextMessageAsync(update.Message.Chat.Id, "Не бывает таких коротких адресов. Помни что тебе также надо указать свои ФИО или что там требует почта в твоей стране.").Wait();
                                        return;
                                    }
                                    santa.Address = address;
                                    santa.Status = Status.Accepted;
                                    db.SaveChanges();
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Отлично! Адрес сохранен. 4 декабря я всех перемешаю и пришлю тебе адрес другого человека которому ты должен будешь отправить подарок.").Wait();
                                    return;
                                }
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Извини, я не понимаю что ты хочешь сделать, попробуй воспользоваться помощью - /help").Wait();
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
            }
        }
    }
}
