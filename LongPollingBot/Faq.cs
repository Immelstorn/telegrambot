using System;
using System.Text;

namespace LongPollingBot
{
    public static class Faq
    {
        public static string Get()
        {
            var sb = new StringBuilder();
            sb.AppendLine("*Q: Что бот делает?*");
            sb.AppendLine("A: Бот собирает людей для проведения локального АДМ(он же Secret Santa)");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Что такое комната?*");
            sb.AppendLine("A: Комната - это компания людей среди которых будет проводиться жеребьевка и раздача адресов для высылки подарков.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Где взять пароль от комнаты?*");
            sb.AppendLine("A: Договоритесь в своей компании о кодовой фразе которая и будет паролем. Когда бот увидит его в первый раз - создаст комнату с этим паролем, а остальных кто его напишет - поместит в нее.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Как вообще этим начать пользоваться?*");
            sb.AppendLine("A:  1. Напиши что-то боту, обычно пишут /start чтобы он понял что ты заинтересован.");
            sb.AppendLine("    2. Выбери язык");
            sb.AppendLine("    3. Бот спросит тебя секретный пароль от комнаты в которой ты хочешь находиться. Напиши ему его. Если ты хочешь создать новую комнату - просто напиши пароль который хочешь использовать в ней.");
            sb.AppendLine("    4. Дальше бот попросит тебя ввести свой адрес на который ты получишь подарок от другого участника. Отправь ему его.");
            sb.AppendLine("    5. Готово! Начинай ждать раздачи адресов и рассылки подарков!");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Как он проверят правильность адреса который я напишу?*");
            sb.AppendLine("A: Очень просто - никак. Тут полная свобода и твоя ответственность. Можешь написать \"на деревню дедушке\", но тогда не жди что тебе что-то приедет.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: А как правильно указать адрес?*");
            sb.AppendLine("A: Указывай адрес с индексом, не забудь ФИО, хорошо на всякий случай указать номер телефона чтоб Санта мог с тобой связаться в случае вопросов.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Ой, я переехал, как поменять адрес?*");
            sb.AppendLine("A: Напиши боту /changeaddress и он позволит тебе сменить адрес.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Я начал регистрацию и передумал, а бот требует адрес и не отстает, как это прекратить?*");
            sb.AppendLine("A: Нужно пройти начальную фазу регистрации - всего два шага - указать пароль от комнаты и адрес, после этого можно будет выйти из этой комнаты если передумал.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: У меня есть три компании в которых я хочу провести Секретного Санту отдельно, это возможно?*");
            sb.AppendLine("A: Да, после того как ты пройдешь начальную регистрацию, ты сможешь добавить еще комнаты с помощью команды /addroom, подробней читай в /help");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Где список команд бота?*");
            sb.AppendLine("A: Напиши ему /help");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Когда мне придет адрес куда отправлять подарок?*");
            sb.AppendLine("A: По умолчанию все в каждой комнате будут перетасованы 4 декабря, но эту дату можно поменять с помощью команды /changedate, подробней читай в /help");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Я отправил подарок, что теперь?*");
            sb.AppendLine("A: Молодец! Теперь надо сообщить своему получателю об этом. Для этого напиши боту \"/sent _пароль от комнаты для которой ты отправил подарок_|_необязательное сообщение для получателя_\". Обрати внимание на разделитель между паролем и сообщением, он важен. В качестве сообщения можно указать трекинговый номер, или просто поздравить с Новым годом. Рекоммендую внутри подарка указать пароль от комнаты из которой ты его отправляешь, чтобы получатель смог отчитаться о получении.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Мне приехал мой подарок, как ообщить об этом моему Деду Морозу?*");
            sb.AppendLine("A: Поздравляю! Напиши боту \"/recieved _пароль от комнаты_\". Если пароля нет, но ты состоишь только в одной комнате, бот засчитает этот подарок в эту комнату.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Ой, я передумал и я на самом деле не отправил подарок, как отменить сообщение про отправку?*");
            sb.AppendLine("A: А никак. Сообщение уже отправлено получателю. Напиши @Immelstorn, попробуем что-то придумать.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Оказалось что подарок который мне приехал, это не подарок, а я уже отчитался что получил, как отменить?*");
            sb.AppendLine("A: Никак, сообщение твоему Санте уже отправлено. Напиши @Immelstorn, глянем что можно сделать.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: У меня ничего не работает, или есть предложение, куда писать?*");
            sb.AppendLine("A: @Immelstorn");
            return sb.ToString();
        }

        public static string GetEnglish()
        {
            var sb = new StringBuilder();
            sb.AppendLine("*Q: What is this bot's purpose?*");
            sb.AppendLine("A: It get's people together to hold an event of local Secret Santa.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: What is the room?*");
            sb.AppendLine("A: Room - is a company of people. Everyone of them will get an address of other random person from ths room to send a gift.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Where should I get a password for the room?*");
            sb.AppendLine("A: You should make an agreement in your company about secret phrase, which will be your password for the room. When bot will see this password for a first time, it will create a new room, and everyone else who will specify the same password will be placed in this room.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: How to use it?*");
            sb.AppendLine("A:  1. Write something to the bot. Usually it is /start.");
            sb.AppendLine("    2. Choose your language.");
            sb.AppendLine("    3. Bot will ask you a password for the room you want to join to. Send it. If you want to create a new room - invent a new password for the room and send it to the bot.");
            sb.AppendLine("    4. Then bot will ask you abot an address that will be used to sent you a gift. Provide it to him.");
            sb.AppendLine("    5. That's it! You can start to wait for the address to send the gift!");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: How will it check that my address is correct?*");
            sb.AppendLine("A: Very simple - it won't. You can write anything there, but beware that if you will write something meaningless - you will not get your gift.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: What is the right way to specify my address?*");
            sb.AppendLine("A: Be sure to specify full address which is needed to get a gift. Including your ZIP code, you full name, and it is a good idea to specify a phone number, in that case your Santa will be able to contact you in the case of problems.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: I've moved, can I change my address?*");
            sb.AppendLine("A: Sure. Send to the bot \"/changeaddress\" and he will allow you to change your address");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: I've started registration and suddenly changed my mind, and the bot continue to ask password or address, how can I stop it?*");
            sb.AppendLine("A: You should finish initial phase - specify room password and address, then you will be able to levae the room if you want.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: I have more than one company which I want to make Secret Santa in, is it possible? *");
            sb.AppendLine("A: Yes, after initial phase of registration you will be able to add additional rooms with /addroom command, use /help for more details");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Where is the commands list?*");
            sb.AppendLine("A: Just ask bot for /help");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: When will I get an address to send the gift?*");
            sb.AppendLine("A: By default everybody will be shuffled 4th of December, but if you want to change this date, you can do this with /changedate command. For details read /help");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: I've sent a gift, what now?*");
            sb.AppendLine("A: Great job! Now you should notify your reciever about it. To do it, send a message to the bot: \"/sent _password for the room you've sent you gift in_|_optional message for reciever_\". Please, consider the divider between password and message, it is important. In the message you may specify track number for you gift, or just wich Happy New Year. I recommend to specify your room's name inside the gift, so you reciever will be able to report about recieveing it.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: I've got my gift, how can I report it to my Santa?*");
            sb.AppendLine("A: Congratulations! Just send it to the bot: \"/recieved _room password_\". If you will not specify a password, and you have only one room - this gift will be counted to that room");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: I've chagned my mind, I didn't send gift, but I've reported about it, how can I undo it?*");
            sb.AppendLine("A: You can't. Your reciever already have been notified. You can send a message to @Immelstorn, and we will try to solve this situation somehow.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: It appeared to be that it is not a gift that I recieved, but I alreadey reported about it, how to undo it?*");
            sb.AppendLine("A: And again - you can't, your Santa was notified about it already. Anyway, send a message to @Immelstorn, and we will see what we can do.");
            sb.AppendLine(Environment.NewLine);
            sb.AppendLine("*Q: Nothing is working, where should I write to?*");
            sb.AppendLine("A: @Immelstorn");
            return sb.ToString();
        }
    }
}