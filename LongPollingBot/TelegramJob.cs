using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;

using LongPollingBot.Models;

using Quartz;
using Quartz.Impl;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LongPollingBot
{
    class TelegramJob : IJob
    {
        private readonly TelegramBotClient _bot = new TelegramBotClient(ConfigurationManager.AppSettings["Token"]);
        private Random _random = new Random();

        public void Execute(IJobExecutionContext context)
        {
            try
            {
                RunTask();
            }
            finally
            {
                ScheduleJob();
            }
        }

        private void RunTask()
        {
            using(var db = new SecretSantaDbContext())
            {
                var rooms = db.Rooms.ToList();
                foreach(var room in rooms)
                {
                    if(room.TimeToSend.AddDays(-1) < DateTime.UtcNow && !room.ReminderSent)
                    {
                        Trace.TraceError($"Sending reminders for room {room.Password}");
                        foreach(var gift in room.Gifts)
                        {
                            if(gift.Santa.ChatId != 0)
                            {
                                if(gift.Santa.Language == Language.Russian)
                                {
                                    _bot.SendTextMessageAsync(gift.Santa.ChatId, $"Привет! Завтра я перемешаю участников в комнате \"{room.Password}\" и разошлю адреса. Пожалуйста, проверь что твой адрес правильный и содержит всю необходимую информацию для того чтобы получить подарок на почте.").Wait();
                                    _bot.SendTextMessageAsync(gift.Santa.ChatId, $"Изменить адрес можно с помощью команды /change").Wait();
                                }
                                else
                                {
                                    _bot.SendTextMessageAsync(gift.Santa.ChatId, $"Hi! I am going to shuffle all participants in the room \"{room.Password}\" and send addresses. Please, check that your address contains all information that is needed for receiving your gift at the post office.").Wait();
                                    _bot.SendTextMessageAsync(gift.Santa.ChatId, $"You can change your address using /change command").Wait();
                                }
                            }
                        }
                        Trace.TraceError($"Reminders are sent for {room.Password}");
                        room.ReminderSent = true;
                        db.SaveChanges();
                    }

                    if(room.TimeToSend < DateTime.UtcNow && !room.MessagesSent)
                    {
                        Trace.TraceError($"Sending recievers for room {room.Password}");
                        room.MessagesSent = true;
                        db.SaveChanges();
                        ShuffleAndSend(room);
                        Trace.TraceError($"Reminders are sent for {room.Password}");
                    }
                }

                var setting = db.Settings.FirstOrDefault();
                var offset = 345894832;
                if(setting == null)
                {
                    db.Settings.Add(new Settings {
                        Offset = 345894832
                    });
                    db.SaveChanges();
                }
                else
                    offset = setting.Offset;

                var updates = _bot.GetUpdatesAsync(offset).Result;
                foreach(var update in updates)
                {
                    ProcessUpdate(update);

                    db.Settings.First().Offset = update.Id + 1;
                    db.SaveChanges();
                }
            }
        }

        public static void ScheduleJob()
        {
            Trace.TraceError($"Scheduling next job");

            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = schedulerFactory.GetScheduler();
            scheduler.Start();

            var job = JobBuilder.Create<TelegramJob>().Build();

            var trigger = TriggerBuilder.Create().StartAt(new DateTimeOffset(DateTime.Now.AddSeconds(5))).Build();

            scheduler.ScheduleJob(job, trigger);
        }

        private void ShuffleAndSend(Room room)
        {
            using(var db = new SecretSantaDbContext())
            {
                var gifts = db.Gifts.Where(g => g.Room.Id == room.Id).ToList();
                gifts = gifts.OrderBy(x => _random.Next()).ToList();
                for(var i = 0; i < gifts.Count - 1; i++)
                {
                    gifts[i].Reciever = gifts[i + 1].Santa;
                    db.SaveChanges();
                }
                gifts[gifts.Count - 1].Reciever = gifts[0].Santa;
                db.SaveChanges();

                foreach(var gift in gifts)
                {
                    if(gift.Santa.ChatId != 0)
                    {
                        if (gift.Santa.Language == Language.Russian)
                        {
                            _bot.SendTextMessageAsync(gift.Santa.ChatId, $"Итак, это время пришло. Твой получатель подарка из комнаты {gift.Room.Password}: {gift.Reciever.Address}").Wait();
                        }
                        else
                        {
                            _bot.SendTextMessageAsync(gift.Santa.ChatId, $"So, it is time to sent gifts! Here is your reciever from the room {gift.Room.Password}: {gift.Reciever.Address}").Wait();
                        }
                    }
                    else
                    {
                        _bot.SendTextMessageAsync(72208686, $"У юзера {gift.Santa.Username} chatId=0. Его получатель: {gift.Reciever.Address}").Wait();
                    }
                }
            }
        }

        private void ProcessUpdate(Update update)
        {
            try
            {
                using(var db = new SecretSantaDbContext())
                {
                    var santa = db.Santas.FirstOrDefault(s => s.ChatId == update.Message.Chat.Id)
                        ?? (update.Message.From.Username != null
                                ? db.Santas.FirstOrDefault(s => s.Username == update.Message.From.Username)
                                : null);


                    if (santa == null)
                    {
                        NewSanta(update, db);
                        return;
                    }

                    if(santa.ChatId == 0)
                    {
                        santa.ChatId = update.Message.Chat.Id;
                        db.SaveChanges();
                    }
                    if (!string.Equals(santa.Username, update.Message.From.Username))
                    {
                        santa.Username = update.Message.From.Username;
                        db.SaveChanges();
                    }
                    if (santa.Status == Status.WaitingForLanguage)
                    {
                        WaitingForLanguage(update, db, santa);
                    }
                    else if(santa.Status == Status.WaitingForPassword)
                    {
                        WaitingForPassword(update, db, santa);
                    }
                    else if(santa.Status == Status.WaitingForAddress)
                    {
                        WaitingForAddress(update, santa, db);
                    }
                    else if(santa.Status == Status.Accepted)
                    {
                        if(string.IsNullOrEmpty(update.Message.Text))
                        {
                            if (santa.Language == Language.Russian)
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Извини, я не понимаю что ты хочешь сделать, попробуй воспользоваться помощью - /help").Wait();
                            }
                            else
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Sorry, I am not very smart and I don't understand what are you saying, try to use /help").Wait();
                            }
                        }
                        else if (update.Message.Text.StartsWith("/help"))
                        {
                            Help(update, santa);
                        }
                        else if(update.Message.Text.StartsWith("/faq"))
                        {
                            _bot.SendTextMessageAsync(update.Message.Chat.Id, santa.Language == Language.Russian ? Faq.Get() : Faq.GetEnglish(), parseMode: ParseMode.Markdown).Wait();
                        }
                        else if(update.Message.Text.StartsWith("/changeaddress"))
                        {
                            santa.Status = Status.WaitingForAddress;
                            db.SaveChanges();
                            if(santa.Language == Language.Russian)
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой новый адрес, на который твой Санта вышлет тебе подарок.").Wait();
                            }
                            else
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Write me your new address, that your Santa will use to send you a gift.").Wait();
                            }
                        }
                        else if(update.Message.Text.StartsWith("/addroom"))
                        {
                            var password = update.Message.Text.Replace("/addroom ", string.Empty);
                            if(string.IsNullOrEmpty(password) || update.Message.Text.Equals("/addroom") || password.Length < 6)
                            {
                                if(santa.Language == Language.Russian)
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 5 символов.").Wait();

                                }
                                else
                                {
                                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Password should be longer the 5 symbols").Wait();
                                }
                                return;
                            }

                            AddRoom(update, db, password, santa);
                        }
                        else if(update.Message.Text.StartsWith("/myinfo"))
                        {
                            MyInfo(update, santa);
                        }
                        else if (update.Message.Text.StartsWith("/info"))
                        {
                            Info(update, santa, db);
                        }
                        else if (update.Message.Text.StartsWith("/participants"))
                        {
                            Participants(update, santa, db);
                        }
                        else if(update.Message.Text.StartsWith("/quit"))
                        {
                            Quit(update, db, santa);
                        }
                        else if (update.Message.Text.StartsWith("/changedate"))
                        {
                            ChangeDate(update, db, santa);
                        }
                        else if(update.Message.Text.StartsWith("/stat") && update.Message.From.Username!= null && update.Message.From.Username.Equals("Immelstorn", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Stat(update, db);
                        }
                        else if(update.Message.Text.StartsWith("/sent"))
                        {
                            Sent(update, db, santa);
                        }
                        else if(update.Message.Text.StartsWith("/received"))
                        {
                            Recieved(update, santa, db);
                        }
                        else
                        {
                            if(santa.Language == Language.Russian)
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Извини, я не понимаю что ты хочешь сделать, попробуй воспользоваться помощью - /help").Wait();
                            }
                            else
                            {
                                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Sorry, I am not very smart and I don't understand what are you saying, try to use /help").Wait();
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Trace.TraceError("================================================================");
                Trace.TraceError($"Update text: {update.Message.Text}, username: {update.Message.From.Username}, chatId: {update.Message.Chat.Id}");
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
            }
        }

        private void WaitingForAddress(Update update, Santa santa, SecretSantaDbContext db)
        {
            var address = update.Message.Text;
            if(string.IsNullOrEmpty(address) || address.Length < 10)
            {
                if(santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Не бывает таких коротких адресов. Помни что тебе также надо указать свои ФИО или что там требует почта в твоей стране.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "I don't think it is real address, it is very short. Remember that you have to use your full address with all the stuff that you country's post is require.").Wait();
                }
                return;
            }
            santa.Address = address;
            santa.Status = Status.Accepted;
            db.SaveChanges();
            if(santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Отлично! Адрес сохранен. Скоро я всех перемешаю и пришлю тебе адрес другого человека, которому ты должен будешь отправить подарок.").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Great! You address is saved. Soon I will shuffle participants and send you address of other participant which you will have to sent gift to.").Wait();
            }
        }

        private void WaitingForPassword(Update update, SecretSantaDbContext db, Santa santa)
        {
            var password = update.Message.Text;
            if(string.IsNullOrEmpty(password) || password.Length < 6)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 5 символов.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Password should be longer than 5 symbols.").Wait();
                }
                return;
            }

            AddRoom(update, db, password, santa);

            santa.Status = Status.WaitingForAddress;
            db.SaveChanges();
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пришли свой адрес, на который твой Санта вышлет тебе подарок.").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Write me your address, that your Santa will use to send you a gift.").Wait();
            }
        }

        private void NewSanta(Update update, SecretSantaDbContext db)
        {
            var newSanta = new Santa
            {
                Username = update.Message.From.Username,
                Status = Status.WaitingForLanguage,
                ChatId = update.Message.Chat.Id
            };
            db.Santas.Add(newSanta);
            db.SaveChanges();
            _bot.SendTextMessageAsync(update.Message.Chat.Id, "Please, choose your language and send me what you have chosen: \nEnglish - /english \nРусский - /russian").Wait();
        }

        private void WaitingForLanguage(Update update, SecretSantaDbContext db, Santa santa)
        {
            if (string.IsNullOrEmpty(update.Message.Text) || !(update.Message.Text.Equals("/russian") || update.Message.Text.Equals("/english")))
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Please, choose your language and send me what you have chosen: \nEnglish - /english \nРусский - /russian").Wait();
                return;
            }
            var lang = update.Message.Text.Equals("/english") ? Language.English : Language.Russian;
            santa.Language = lang;
            santa.Status=Status.WaitingForPassword;
            db.SaveChanges();

            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Судя по всему ты тут новенький.\nС принципиами Secret Santa ты, я надеюсь, знаком.\nЗдесь люди объединяются по комнатам с помощью секретного пароля и собираются дарить друг другу подарки.\n").Wait();
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Введи пароль к существующей комнате или новый пароль для создания новой комнаты.").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Looks like you're new here. \nI hope you know what Secret Santa is. \nPeople are combining by rooms using secret password and they are going to sent gifts to each other anonymously.").Wait();
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Send me the password for the room you want to join, if room with this password doesn't exist it will be created for you.").Wait();
            }
        }

        private void Help(Update update, Santa santa)
        {
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "/help - помощь \n/faq - подробный список вопросов и ответов на них \n/changeaddress - сменить адрес \n/addroom _пароль к комнате_ - добавить комнату \n/myinfo - посмотреть свой адрес и комнаты, в которых ты находишься \n/info _пароль к комнате_ - узнать информацию о комнате \n/participants _пароль к комнате_ - узнать кто находится в комнате \n/changedate _пароль к комнате_|_новая дата_ - сменить дату рассылки адресов в этой комнате. Это может сделать только создатель комнаты. Формат даты: yyyy-mm-dd \n/quit _пароль к комнате_ - выйти из комнаты", parseMode: ParseMode.Markdown).Wait();
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"После того как я разошлю адреса, можно будет воспользоваться следующими командами: \n\n/sent _пароль к комнате_|_сообщение для получателя_ - сообщить получателю что подарок в пути, можно добавить сообщение для получателя, например с трек-номером. \n*Обрати внимание на разделитель между паролем к комнате и сообщением* \n\n/recieved _пароль к комнате_ - сообщить Санте что подарок получен \n\nСтоит помнить что эти команды одноразовые и отменить их действие нельзя.", parseMode: ParseMode.Markdown).Wait();
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Если у тебя есть вопросы/пожелания или ты заметил какие-то баги - напиши, пожалуйста, пользователю @Immelstorn").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "/help - help \n/faq - frequently asked questions and answers \n/changeaddress - change your address \n/addroom _room password_ - add room \n/myinfo - check your address and rooms which you are in \n/info _room password_ - check room's information \n/participants _room password_ - see who are in this room \n/changedate _room password_|_new date_ - change date for addresses shuffling. Only room's creator can do this. Date format: yyyy-mm-dd \n/quit _room password_ - leave the room", parseMode: ParseMode.Markdown).Wait();
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"When addresses will be sent, you will be able to use next commands:\n\n/sent _room password_|_message for reciever_ - notify your reciever that you sent the gift. You may also add a message, with track number or wish Happy New Year \n*Pay attention to the divider between room password and message. It is imporant.* \n\n/recieved _room password_ - notify your Santa that you've got the gift \n\nYou should now that these commands are one time and you can't undo them.", parseMode: ParseMode.Markdown).Wait();
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"If you have any questions/suggestions or you noticed some bugs, please send a message to @Immelstorn").Wait();
            }
        }

        private void Stat(Update update, SecretSantaDbContext db)
        {
            var santas = db.Santas.Count();
            var rooms = db.Rooms.Count();
            _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Santas: {santas}, rooms: {rooms}").Wait();
        }

        private void MyInfo(Update update, Santa santa)
        {
            var rooms = "";
            if(santa.Gifts.Any())
            {
                rooms = santa.Gifts.Aggregate(string.Empty, (current, gift) => current + $" {gift.Room.Password},");
                rooms = rooms.Substring(1, rooms.Length - 2);
            }
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Твой текущий адрес: {santa.Address}, твои комнаты: {rooms}").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Your current address: {santa.Address}, your rooms: {rooms}").Wait();
            }
        }

        private void Info(Update update, Santa santa, SecretSantaDbContext db)
        {
            var password = update.Message.Text.Replace("/info ", string.Empty);
            if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/info") || password.Length < 6)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 5 символов.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Password should be longer than 5 symbols").Wait();
                }
                return;
            }
            var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
            if (room == null || santa.Gifts.All(g => g.Room.Id != room.Id))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует или у тебя нет к ней доступа, ты можешь добавить ее с помощью комманды /addroom <пароль к комнате>").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Such room doesn't exist or you do not have access to it. You can create it with /addroom <room password>").Wait();
                }
                return;
            }

            var count = db.Santas.Count(s => s.Gifts.Any(g => g.Room.Id == room.Id));
            var creator = room.Creator?.Username ?? string.Empty;
            var date = room.TimeToSend;
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Cейчас тут {count} человек.\nСоздатель: {creator}.\nДата рассылки адресов: {date:yyyy-MM-dd}").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Right now it is {count} people here.\nCreator is {creator}.\nDate for addresses sending: {date:yyyy-MM-dd}").Wait();
            }
        }

        private void Participants(Update update, Santa santa, SecretSantaDbContext db)
        {
            var password = update.Message.Text.Replace("/participants ", string.Empty);
            if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/participants") || password.Length < 6)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 5 символов.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Password should be longer than 5 symbols").Wait();
                }
                return;
            }
            var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
            if (room == null || santa.Gifts.All(g => g.Room.Id != room.Id))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует или у тебя нет к ней доступа, ты можешь добавить ее с помощью комманды /addroom <пароль к комнате>").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Such room doesn't exist or you do not have access to it. You can create it with /addroom <room password>").Wait();
                }
                return;
            }


            var santas = room.Gifts.Select(g => g.Santa).ToList(); //db.Santas.Where(s => s.Gifts.Any(g => g.Room.Id == room.Id)).ToList();
            var sb = new StringBuilder();
            var noUsername = santa.Language == Language.Russian ? "Санта без юзернейма ¯\\_(ツ)_/¯" : "Santa without a username ¯\\_(ツ)_/¯";
            foreach (var s in santas)
            {
                sb.AppendLine($"@{s.Username ?? noUsername}");
            }

            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Вот кто сейчас находится в комнате:").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Here are participants in this room:").Wait();
            }
            _bot.SendTextMessageAsync(update.Message.Chat.Id, sb.ToString()).Wait();
        }

        private void Quit(Update update, SecretSantaDbContext db, Santa santa)
        {
            var password = update.Message.Text.Replace("/quit ", string.Empty);
            if(string.IsNullOrEmpty(password) || update.Message.Text.Equals("/quit") || password.Length < 6)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 5 символов.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Password should be longer than 5 symbols").Wait();
                }
                return;
            }
            var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
            if(room == null || santa.Gifts.All(g => g.Room.Id != room.Id))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Such room doesn't exist").Wait();
                }
                return;
            }

            if(room.MessagesSent)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Извини, но адреса уже разосланы, ты не можешь покинуть комнату").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Sorry, but I've already shuffled people in this room, you can't leave it.").Wait();
                }
                return;
            }

            var gift = santa.Gifts.First(g => g.Room.Id == room.Id);
            db.Gifts.Remove(gift);
            db.SaveChanges();
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Очень жаль. Передумаешь - возвращайся.").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "It's a pity! If you will change your mind - you're welcome!").Wait();
            }
        }

        private void ChangeDate(Update update, SecretSantaDbContext db, Santa santa)
        {
            var parameters = update.Message.Text.Replace("/changedate ", string.Empty).Split('|');

            if (parameters.Length < 2 || update.Message.Text.Equals("/changedate"))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты забыл указать пароль к комнате и новую дату!").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"You forgot to specify room password and new date!").Wait();
                }
                return;
            }

            var password = parameters[0];
            if (string.IsNullOrEmpty(password) || update.Message.Text.Equals("/changedate") || password.Length < 6)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Пароль должен быть длиннее 5 символов.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Password should be longer than 5 symbols").Wait();
                }
                return;
            }

            var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
            if (room == null || santa.Gifts.All(g => g.Room.Id != room.Id))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Such room doesn't exist").Wait();
                }
                return;
            }

            if(!room.Creator.Id.Equals(santa.Id))
            {
                var noUsername = santa.Language == Language.Russian ? "Санта без юзернейма ¯\\_(ツ)_/¯" : "Santa without a username ¯\\_(ツ)_/¯";

                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Только создатель комнаты может менять дату. Создатель: {room.Creator.Username ?? noUsername}").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Only creator of the room can change the date. Creator is {room.Creator.Username ?? noUsername}").Wait();
                }
                return;
            }

            DateTime newDate;
            if(!DateTime.TryParse(parameters[1], out newDate) || newDate < DateTime.UtcNow)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Дата в неверном формате или в прошлом. Попробуй такой формат: yyyy-mm-dd. Дата должна быть в UTC.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"The date you have entered is invalid or in the past. Try this format: yyyy-mm-dd. Date should be in UTC.").Wait();
                }
                return;
            }
            room.TimeToSend = newDate;
            room.ReminderSent = false;
            room.MessagesSent = false;
            db.SaveChanges();
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Готово. Новая дата: {newDate:yyyy-MM-dd}").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"It's done. New date is {newDate:yyyy-MM-dd}").Wait();
            }
        }

        private void Sent(Update update, SecretSantaDbContext db, Santa santa)
        {
            var parameters = update.Message.Text.Replace("/sent ", string.Empty).Split('|');

            if(parameters.Length == 0 || update.Message.Text.Equals("/sent"))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты забыл указать пароль к комнате и сообщение для получателя!").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"You forgot to specify room password and/or message for reciever!").Wait();
                }
                return;
            }

            var password = parameters[0];
            var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
            if(room == null || santa.Gifts.All(g => g.Room.Id != room.Id))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Such room doesn't exist").Wait();
                }
                return;
            }

            if(!room.MessagesSent)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Я еще не рассылал адреса по этой комнате, о чем ты собрался отчитываться?").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "I didn't sent any addresses in this room yet, what are you going to report about?").Wait();
                }
                return;
            }

            var gift = santa.Gifts.First(g => g.Room.Id == room.Id);

            if(gift.Sent)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Ты уже отчитывался ранее об отправке этого подарка, спасибо!").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "You have alreade reported about sending of this gift, thanks!").Wait();
                }
                return;
            }

            gift.Sent = true;
            gift.SentDate = DateTime.UtcNow;

            if(parameters.Length == 2)
            {
                gift.MessageFromSanta = parameters[1];
            }
            db.SaveChanges();
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Спасибо! Я немедленно передам эту радостную новость получателю! С Новым годом!").Wait();
                _bot.SendTextMessageAsync(gift.Reciever.ChatId, $"Ура! Тебе отправили подарок из комнаты \"{gift.Room.Password}\"! Начинайте ждать и уважать почту! С Новым годом!").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Thanks! I will notify reciever immediately! Happy New Year!").Wait();
                _bot.SendTextMessageAsync(gift.Reciever.ChatId, $"Yay! Your gift from the room \"{gift.Room.Password}\" was sent! You should start to wait and pay respect to the post service. Happy New Year!").Wait();
            }

            if (!string.IsNullOrEmpty(gift.MessageFromSanta))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(gift.Reciever.ChatId, $"Твой Санта также оставил для тебе сообщение. Вот оно: {gift.MessageFromSanta}").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(gift.Reciever.ChatId, $"Your Santa also left a message for you. Here it is: {gift.MessageFromSanta}").Wait();
                }
            }
        }

        private void Recieved(Update update, Santa santa, SecretSantaDbContext db)
        {
            Room room = null;
            var password = update.Message.Text.Replace("/received ", string.Empty);
            if(string.IsNullOrEmpty(password) || update.Message.Text.Equals("/received") || password.Length < 6)
            {
                if(santa.Gifts.Count > 1)
                {
                    if (santa.Language == Language.Russian)
                    {
                        _bot.SendTextMessageAsync(update.Message.Chat.Id, "Ты состоишь более чем в одной комнате, укажи пароль к комнате из которой ты получил подарок. Если ты не знаешь точно - что ж, ты не сможешь сообщить о получении. Помни, что пароль должен быть длиннее 5 символов.").Wait();
                    }
                    else
                    {
                        _bot.SendTextMessageAsync(update.Message.Chat.Id, "You are in more the one room, please, specify a room password for a room that you get your gift from. If you're not sure, well, you will not be able to report about this gift, unfortunately. Remember, that a password should be longer then 5 symbols.").Wait();
                    }
                    return;
                }

                if(santa.Gifts.Count == 1)
                {
                    room = santa.Gifts.First().Room;
                }
            }

            if(room == null)
            {
                room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
            }
            if(room == null || santa.Gifts.All(g => g.Room.Id != room.Id))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Такая комната не существует").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Such room doesn't exist").Wait();
                }
                return;
            }
            if(!room.MessagesSent)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Я еще не рассылал адреса по этой комнате, о чем ты собрался отчитываться?").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "I didn't sent any addresses in this room yet, what are you going to report about?").Wait();
                }
                return;
            }
            var gift = santa.Gifts.First(g => g.Room.Id == room.Id);

            if(gift.Recieved)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Ты уже отчитывался ранее о получении этого подарка, спасибо!").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "You have alreade reported about recieving this gift, thanks!").Wait();
                }
                return;
            }

            if(!gift.Sent)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Санта не указал что он отправил подарок, ты не можешь подтвердить его получение.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, "Santa still didn't marked that gift as sent, so you can't mark it as recieved.").Wait();
                }
                return;
            }

            gift.Recieved = true;
            gift.RecievedDate = DateTime.UtcNow;
            db.SaveChanges();
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Ура! Я немедленно передам эту радостную новость твоему Санте! С Новым годом!").Wait();
                _bot.SendTextMessageAsync(gift.Santa.ChatId, $"Миссия выполнена! Твой подарок благополучно дошел получателю из комнаты \"{gift.Room.Password}\"!").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, "Yay! I will notify your Santa immediately! Happy new Year!").Wait();
                _bot.SendTextMessageAsync(gift.Santa.ChatId, $"Mission accomplished! Your gift for the room \"{gift.Room.Password}\" was successfully recieved!").Wait();
            }
        }

        private void AddRoom(Update update, SecretSantaDbContext db, string password, Santa santa)
        {
            var room = db.Rooms.FirstOrDefault(r => r.Password.Equals(password));
            if(room == null)
            {
                var gift = new Gift {
                    Santa = santa,
                    Room = new Room {
                        Password = password,
                        Creator = santa
                    }
                };

                db.Gifts.Add(gift);
                db.SaveChanges();
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Новая комната создана. Раздача адресов назначена на {gift.Room.TimeToSend:yyyy-MM-dd}. Приглашай друзей с помощью пароля для комнаты.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"New room is created. Shuffling and addresses sending are scheduled to {gift.Room.TimeToSend:yyyy-MM-dd}. Invite your friends using this room's password.").Wait();
                }
                return;
            }

            if(DateTime.Now > room.TimeToSend && room.MessagesSent)
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Извини, но в этой комнате уже обменялись адресами и вход для новых людей закрыт. Попробуй создать новую комнату.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Sorry, but people in this room was already shuffled, you can't join. You can create a new room, though.").Wait();
                }
                return;
            }

            var count = db.Santas.Count(s => s.Gifts.Any(g => g.Room.Id == room.Id));
            if(santa.Gifts.Any(g => g.Room.Id == room.Id))
            {
                if (santa.Language == Language.Russian)
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты уже добавлен в эту комнату, сейчас тут {count} человек.").Wait();
                }
                else
                {
                    _bot.SendTextMessageAsync(update.Message.Chat.Id, $"You are already in this room. It is {count} people here right now.").Wait();
                }
                return;
            }

            var newgift = new Gift {
                Santa = santa,
                Room = room
            };

            santa.Gifts.Add(newgift);
            db.SaveChanges();
            if (santa.Language == Language.Russian)
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"Ты добавлен в эту комнату, сейчас тут {++count} человек.").Wait();
            }
            else
            {
                _bot.SendTextMessageAsync(update.Message.Chat.Id, $"You've been added to this room, it is {++count} people here right now.").Wait();
            }
        }
    }
}
