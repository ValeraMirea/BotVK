using System;
using System.Collections.Generic;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bot.DataBase;
using VkNet.Model.Keyboard;
using Microsoft.Data.SqlClient;
using VkNet.Model.Attachments;
using Bot;
using Npgsql;
using System.Text;
using VkNet.Enums.Filters;

namespace TestVkBot
{
    public class Program
    {
        static string[] Commands = { "+реп", "/профиль", "/рейтинг", "/команды", "/жалоба", "/предложение", "-реп", "/рулетка",
            "/предупреждение", "/бан", "/покупка репутации", "да", "нет" ,"Faq",  "Информация об истории университета", "Расписание занятий", "Расположение кампусов",
            "Вернуться Назад", "Карта Кампуса В-78", "Карта Кампуса В-86","Карта Кампуса МП-1","Карта Кампуса 1-й Щипковский пер., д. 23","Карта Кампуса С-20","Карта Кампуса СГ-22",
            "Карта Кампуса ул. Усачева, д.7/1"};

        public static string Token => File.ReadAllText("Token_bot.txt"); //Токен для работы бота
        public static ulong Group_Id => ulong.Parse(File.ReadAllText("ID_Group.txt"));   // Ид вашего сообщества или паблиа
        public static string Login => File.ReadAllText("Login.txt"); //Токен для работы бота от имени админа
        private static int lastRandomId = 0;
        private static Dictionary<long, double> User_Data = new Dictionary<long, double>();

        [Obsolete]
        public static void Main(string[] args)
        {
            var api = new VkApi();
            api.Authorize(new ApiAuthParams() { AccessToken = Token });
            var s = api.Groups.GetLongPollServer(Group_Id);
            string ts = s.Ts;
            var User_Admin = new VkApi();
            User_Admin.Authorize(new ApiAuthParams() { AccessToken = Login});
            try
            {
                List<Tuple<String, double>> Rating = new List<Tuple<String, double>>();
                while (true)
                {
                    var poll = api.Groups.GetBotsLongPollHistory(
                        new BotsLongPollHistoryParams()
                        { Server = s.Server, Ts = ts, Key = s.Key, Wait = 1 });

                    if (poll?.Updates == null) continue;
                    long? forwardId = null;
                    string forwardName = "";
                    string LastName = "";
                    foreach (var a in poll.Updates)
                    {

                        if (a.Type == GroupUpdateType.MessageNew)
                        {
                            string messageText = a.MessageNew.Message.Text;
                            if (a.MessageNew.Message.PeerId >= 2000000000) // Считает сообщения только внутри беседы, в личных сообщениях не будет считать
                            {
                                forwardId = a.MessageNew.Message.FromId;
                                long User_Rep_ID = (long)forwardId;

                                // работа с БД
                                // создаем подключение
                                using ApplicationContext db = new ApplicationContext();

                                // получаем информацию о пользователе
                                VkUser VkUser = null;
                                List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                             where user.Id.Equals(User_Rep_ID)
                                                             select user).ToList();      // ищем пользователя с таким ID

                                if (FoundedUsers.Count > 0) // если пользотватель найден 
                                {
                                    VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Прост овытаскиваем пользователя из массива
                                }
                                else    // если пользователя нет в базе данных
                                {
                                    VkUser = new VkUser();      // создаем пользователя
                                    VkUser.Id = User_Rep_ID;    // задаем ему ID по ВК
                                                                //VkUser.FirstName = forwardName;     // задаем имя
                                                                //VkUser.LastName = LastName; // Задаем фамилимю 
                                    db.VkUsers.Add(VkUser);     // добавляем в базу данных
                                }

                                // добавляем сообщение
                                VkUser.Number_of_messages += 1;

                                // сохраняем в БД
                                db.SaveChanges();
                            }
                            int commandId = -1; // id команды
                            for (int i = 0; i < Commands.Length; i++)   // проверяем соответвие текста сообщения одной из команд
                            {
                                if (messageText == null)
                                {
                                    Console.WriteLine("messageText == null");
                                    continue;
                                }
                                if (messageText.Contains(Commands[i]))
                                {
                                    commandId = i;  // сохряняем id команды
                                    break;
                                }
                            }
                            var keyboard = new KeyboardBuilder()
                            .AddButton("Информация об истории университета", "btnValue", KeyboardButtonColor.Primary)
                            .SetInline(false)
                            .SetOneTime()
                            .AddLine()
                            .AddButton("Расписание занятий", "btnValue", KeyboardButtonColor.Primary)
                            .SetOneTime()
                            .AddLine()
                            .AddButton("Расположение кампусов", "btnValue", KeyboardButtonColor.Primary)
                            .Build();
                            switch (commandId)
                            {
                                case 0: // +реп
                                     
                                    Message replyMessage = a.MessageNew.Message.ReplyMessage; // получаем id того, в ответ на сообщение котором удаётся +реп

                                    string sendMessageText = "Вы не указали, кому даёте +реп"; // сообщение, которое отправится пользователю (дефолтное значение - ошибка)
                                    long User_Rep_ID = (long)forwardId;
                                    VkUser VkUser = null;

                                    // работа с БД
                                    // создаем подключение
                                    {
                                        using ApplicationContext db = new ApplicationContext();

                                        // получаем информацию о пользователе
                                        
                                        List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                                     where user.Id.Equals(User_Rep_ID)
                                                                     select user).ToList();      // ищем пользователя с таким ID

                                        if (FoundedUsers.Count > 0) // если пользотватель найден 
                                        {
                                            VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Прост овытаскиваем пользователя из массива
                                        }
                                        else    // если пользователя нет в базе данных
                                        {
                                            VkUser = new VkUser();      // создаем пользователя
                                            VkUser.Id = User_Rep_ID;    // задаем ему ID по ВК
                                                                        //VkUser.FirstName = forwardName;     // задаем имя
                                                                        //VkUser.LastName = LastName; // Задаем фамилимю 
                                            db.VkUsers.Add(VkUser);     // добавляем в базу данных
                                        }

                                        // добавляем репутацию
                                        VkUser.Affordable_reputation -= 1;

                                        // сохраняем в БД
                                        db.SaveChanges();
                                    }
                                   
                                    if (VkUser.Affordable_reputation <= 0)
                                    {
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = $"У вас закончилась доступная репутация &#128532;",
                                            RandomId = getRandomMessageId()

                                        });
                                    }
                                    else
                                    {
                                        if (replyMessage == null) // если нет ответа на сообщение, значит пытаемся вытащить через @
                                        {
                                            // ищем через регулярное выражение, кому отправляют +реп
                                            Regex regex = new Regex(@"[0-9]*\|");
                                            MatchCollection matches = regex.Matches(messageText);

                                            if (matches.Count > 0) // если в сообщении указан ID
                                            {
                                                Regex regex_1 = new Regex(@"club[0-9]*\|");
                                                MatchCollection match = regex_1.Matches(messageText);
                                                if (match.Count > 0)
                                                {
                                                    sendMessageText = $"Выдавать и упоминать ботов - это неуважение к собеседникам 👊🤖";
                                                }
                                                else
                                                {
                                                    // на входе будет найдено что-то вида 122345|, убираем | и конвертируем в long
                                                    forwardId = long.Parse(matches[0].Value.Replace("|", "")); // вытаскиваем ID того, кому даётся репутация
                                                    forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем реп
                                                    LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;                                                                                       //sendMessageText =

                                                }
                                            }
                                        }
                                        else // если это ответ на какое-то сообщение
                                        {
                                            if (replyMessage.FromId < 0)
                                            {
                                                sendMessageText = $"Я всего лишь бот. Мне не нужна ваша репутация";
                                            }
                                            else
                                            {
                                                forwardId = replyMessage.FromId; // вытаскиваем ID того, кому даётся репутация
                                                forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем реп
                                                LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                            }
                                        }


                                        if (forwardId != null && forwardId.Equals(a.MessageNew.Message.FromId))
                                        {
                                            Random light = new Random();
                                            int answer = light.Next(1, 4);
                                            switch (answer)
                                            {
                                                case 0:
                                                    sendMessageText = $"&#128552; Что-то здесь не так, а вот что остается загадкой и для тебя, и для меня &#128521;";
                                                    break;
                                                case 1:
                                                    sendMessageText = $"&#128552; Вы пишете загадодками написано, пойду к Гадалке схожу, пусть она мне раскроет мне эту тайну &#128302;";
                                                    break;
                                                case 2:
                                                    sendMessageText = $"Полисмены проделали блестящую работу, уничтожив любые улики… " + "\r\n" + "Жаль, что я не шерлок Холмс, чтобы раскрыть ваш обман";
                                                    break;
                                                case 3:
                                                    sendMessageText = $"Нельзя поднимать репутацию самому себе, у нас честные соревнования!";
                                                    break;

                                            }
                                        }
                                        else if (forwardId != null)
                                        {
                                            User_Rep_ID = (long)forwardId;

                                            // работа с БД
                                            // создаем подключение
                                            using ApplicationContext db = new ApplicationContext();

                                            // получаем информацию о пользователе
                                            VkUser = null;
                                            List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                                         where user.Id.Equals(User_Rep_ID)
                                                                         select user).ToList();      // ищем пользователя с таким ID

                                            if (FoundedUsers.Count > 0) // если пользотватель найден 
                                            {
                                                VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Прост овытаскиваем пользователя из массива
                                            }
                                            else    // если пользователя нет в базе данных
                                            {
                                                VkUser = new VkUser();      // создаем пользователя
                                                VkUser.Id = User_Rep_ID;    // задаем ему ID по ВК
                                                                            //VkUser.FirstName = forwardName;     // задаем имя
                                                                            //VkUser.LastName = LastName; // Задаем фамилимю 
                                                db.VkUsers.Add(VkUser);     // добавляем в базу данных
                                            }

                                            // добавляем репутацию
                                            VkUser.Rating += 0.3;
                                            VkUser.Rating = Math.Round(VkUser.Rating, 3);

                                            // сохраняем в БД
                                            db.SaveChanges();

                                            sendMessageText =
                                                $"&#127942;Уважение оказано. Плюс 0,3 добавляется к карме пользователя [id{forwardId}|{forwardName} {LastName}]. Текущий рейтинг: {Math.Round(VkUser.Rating, 3, MidpointRounding.AwayFromZero)}";
                                        }
                                    }                                    

                                    api.Messages.Send(new MessagesSendParams()
                                    {
                                        PeerId = a.MessageNew.Message.PeerId,
                                        UserId = a.MessageNew.Message.UserId,
                                        ChatId = a.MessageNew.Message.ChatId,
                                        Message = sendMessageText,
                                        RandomId = getRandomMessageId()

                                    });
                                    break;

                                case 1: //профиль


                                    replyMessage = a.MessageNew.Message.ReplyMessage; // получаем id того, в ответ на сообщение котором удаётся +реп

                                    //sendMessageText = "Вы не указали чей профиль вы хотите посмотреть"; // сообщение, которое отправится пользователю (дефолтное значение - ошибка)

                                    forwardId = null;
                                    forwardName = "";
                                    LastName = "";

                                    if (replyMessage == null) // если нет ответа на сообщение, значит пытаемся вытащить через @
                                    {
                                        forwardId = a.MessageNew.Message.FromId; // вытаскиваем ID того, чей профиль хотим посмотреть
                                        forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].FirstName; // получаем Имя того, кому даем реп
                                        LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].LastName;
                                        using ApplicationContext db = new ApplicationContext();

                                        var currentUser = (from user in db.VkUsers
                                                           where user.Id == forwardId
                                                           select user).FirstOrDefault();

                                        string userText = currentUser == null
                                                          ? $"Пользователь с ID {forwardId} не найден."
                                                          : $"&#128210; Профиль [id{currentUser.Id}|{forwardName} {LastName}]\r\n" +
                                                            $"&#127942; Текущий счет: {currentUser.Rating}" + "\r\n" +
                                                            $"&#128529; Число предупреждений: {currentUser.Warnings}" + "\r\n" +
                                                            $"&#128526; Число доступных репутаций: {currentUser.Affordable_reputation}" + "\r\n" +
                                                            $"&#9993; Число сообщений: {currentUser.Number_of_messages}";

                                        sendMessageText = $"\r\n{userText}";
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }
                                    else // если это ответ на какое-то сообщение
                                    {
                                        if (replyMessage.FromId < 0)
                                        {
                                            sendMessageText = $"Я всего лишь бот. Вам не нужен мой профиль";
                                        }
                                        else
                                        {
                                            forwardId = replyMessage.FromId; // вытаскиваем ID того, чей профиль хотим посмотреть
                                            forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].FirstName; // получаем Имя того, кому даем реп
                                            LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].LastName;
                                            using ApplicationContext db = new ApplicationContext();

                                            var currentUser = (from user in db.VkUsers
                                                               where user.Id == forwardId
                                                               select user).FirstOrDefault();

                                            string userText = currentUser == null
                                                          ? $"Пользователь с ID {forwardId} не найден."
                                                          : $"&#128210; Профиль [id{currentUser.Id}|{forwardName} {LastName}]\r\n" +
                                                            $"&#127942; Текущий счет: {currentUser.Rating}" + "\r\n" +
                                                            $"&#128529; Число предупреждений: {currentUser.Warnings}" + "\r\n" +
                                                            $"&#128526; Число доступных репутаций: {currentUser.Affordable_reputation}" + "\r\n" +
                                                            $"&#9993; Число сообщений: {currentUser.Number_of_messages}";
                                            sendMessageText = $"\r\n{userText}";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                RandomId = getRandomMessageId()
                                            });
                                        }
                                    }
                                    break;
                                case 2://рейтинг
                                    {
                                        using ApplicationContext db = new ApplicationContext();

                                        List<VkUser> topUsers = db.VkUsers.OrderByDescending(x => x.Rating).Take(15).ToList();
                                        string topUsersText = "";

                                        // Перебираем топ пользователей
                                        foreach (var user in topUsers)
                                        {
                                            // Получаем информацию о пользователе из API ВКонтакте
                                            User vkUser = api.Users.Get(new[] { user.Id }, VkNet.Enums.Filters.ProfileFields.FirstName | VkNet.Enums.Filters.ProfileFields.LastName)[0];

                                            // Выводим имя и фамилию пользователя, а также его рейтинг
                                            topUsersText = string.Join("\r\n", topUsers.Select((users, i) => $"№{i + 1}. [id{users.Id}|{vkUser.FirstName} {vkUser.LastName}]: {users.Rating}"));

                                        }
                                        sendMessageText = $"&#127937; Топ 15 пользователей:\r\n{topUsersText}";
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }
                                  
                                    break;
                                case 3: //команды
                                    api.Messages.Send(new MessagesSendParams()
                                    {
                                        PeerId = a.MessageNew.Message.PeerId,
                                        UserId = a.MessageNew.Message.UserId,
                                        ChatId = a.MessageNew.Message.ChatId,
                                        Message = $"Список доступных команд:" + "\r\n" + "\r\n" +
                                        $"&#128526; +реп - поднимаешь репутацию собеседнику" + "\r\n" + "\r\n" +
                                        $"&#128520; -реп - снимаешь репутацию собеседнику" + "\r\n" + "\r\n" +
                                        $"&#128100; /профиль - показывает профиль в беседе" + "\r\n" + "\r\n" +
                                        $"&#129312; /рулетка - испытай свою удачу в русской рулетке" + "\r\n" + "\r\n" +
                                        $"&#128695; /жалоба - подать жалобу на собеседника" + "\r\n" + "\r\n" +
                                        $"&#128736; /предложение - внести улучшения/изменения в работу бота" + "\r\n" + "\r\n" +
                                        $"&#128176; /покупка репутации - волшебный магазин, где можно обменять опыт на репутацию" + "\r\n" + "\r\n" +
                                        $"&#10067; Faq - получение информации на самые часто задоваемые вопросы" + "\r\n" + "\r\n" +
                                        $"Приятного общения &#128521;",
                                        RandomId = getRandomMessageId()
                                    });
                                    break;
                                case 4: //жалоба
                                    replyMessage = a.MessageNew.Message.ReplyMessage;
                                    sendMessageText = "Вы не указали, на кого хотите подать жалобу"; // сообщение, которое отправится пользователю (дефолтное значение - ошибка)

                                    forwardId = null;
                                    forwardName = "";
                                    LastName = "";
                                    var forwardId_1 = a.MessageNew.Message.FromId; // вытаскиваем ID того, кто кидает жалобу
                                    var forwardName_1 = api.Users.Get(new[] { (long)forwardId_1 }, null, NameCase.Nom)[0].FirstName;
                                    var LastName_1 = api.Users.Get(new[] { (long)forwardId_1 }, null, NameCase.Nom)[0].LastName;
                                    if (replyMessage == null) // если нет ответа на сообщение, значит пытаемся вытащить через @
                                    {
                                        // ищем через регулярное выражение, кому отправляют жалобу
                                        Regex regex = new Regex(@"[0-9]*\|");
                                        MatchCollection matches = regex.Matches(messageText);

                                        if (matches.Count > 0) // если в сообщении указан ID
                                        {
                                            Regex regex_1 = new Regex(@"club[0-9]*\|");
                                            MatchCollection match = regex_1.Matches(messageText);
                                            if (match.Count > 0)
                                            {
                                                sendMessageText = $"Выдавать и упоминать ботов - это неуважение к собеседникам 👊";
                                            }
                                            else
                                            {
                                                // на входе будет найдено что-то вида 122345|, убираем | и конвертируем в long
                                                forwardId = long.Parse(matches[0].Value.Replace("|", "")); // вытаскиваем ID того, кому даётся жалоба
                                                forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем жалобу
                                                LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                            }


                                        }
                                    }
                                    else // если это ответ на какое-то сообщение
                                    {
                                        if (replyMessage.FromId < 0)
                                        {
                                            sendMessageText = $"Я всего лишь бот. За что на меня жаловаться - то?!";
                                        }
                                        else
                                        {
                                            forwardId = replyMessage.FromId; // вытаскиваем ID того, кому даётся репутация
                                            forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем жалобу
                                            LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                        }
                                    }


                                    if (forwardId != null && forwardId.Equals(a.MessageNew.Message.FromId))
                                    {
                                        Random light = new Random();
                                        int answer = light.Next(1, 4);
                                        switch (answer)
                                        {
                                            case 0:
                                                sendMessageText = $"&#128552; Что-то здесь не так, а вот что остается загадкой и для тебя, и для меня &#128521;";
                                                break;
                                            case 1:
                                                sendMessageText = $"&#128552; Вы пишете загадодками написано, пойду к Гадалке схожу, пусть она мне раскроет мне эту тайну &#128302;";
                                                break;
                                            case 2:
                                                sendMessageText = $"Полисмены проделали блестящую работу, уничтожив любые улики… " + "\r\n" + "Жаль, что я не шерлок Холмс, чтобы раскрыть ваш обман";
                                                break;
                                            case 3:
                                                sendMessageText = $"Чем ты сам себе не угодил? Пойди и выспесь лучше)";
                                                break;

                                        }
                                    }
                                    else if (forwardId != null)
                                    {
                                        //long User_Req_ID = (long)forwardId;
                                        //if (!User_Data.ContainsKey(User_Req_ID))
                                        //{
                                        //    User_Data[User_Req_ID] = 0.0;
                                        //}
                                        //double User_Req = User_Data[User_Req_ID];
                                        //User_Req += 1;
                                        //User_Data[User_Req_ID] = Math.Round(User_Req, 3, MidpointRounding.AwayFromZero);
                                        sendMessageText =
                                            $"Пользователь [id{forwardId_1}|{forwardName_1} {LastName_1}] подает жалобу на пользователя [id{forwardId}|{forwardName} {LastName}], Ожидайте решение администратора беседы";

                                    }

                                    api.Messages.Send(new MessagesSendParams()
                                    {
                                        PeerId = a.MessageNew.Message.PeerId,
                                        UserId = a.MessageNew.Message.AdminAuthorId,
                                        //UserId = a.MessageNew.Message.UserId,
                                        ChatId = a.MessageNew.Message.ChatId,
                                        Message = sendMessageText,
                                        //ForwardMessages =Conversation(lastRandomId),
                                        RandomId = getRandomMessageId()

                                    });
                                    //api.Groups.GetMembers(new GroupsGetMembersParams()
                                    //{
                                    //    GroupId = Group_Id.ToString(),
                                    //    Count = 50,
                                    //    Offset = 0,
                                    //    Fields = UsersFields.All,
                                    //    Filter = GroupsMemberFilters.Managers,
                                    //});
                                    // replyMessage = a.MessageNew.Message.ReplyMessage;
                                    // forwardId = replyMessage.FromId; // вытаскиваем ID того, на кого подают жалобу
                                    // forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName;//получаем имя 
                                    //var forwardId_1 = a.MessageNew.Message.FromId; // вытаскиваем ID того, кто кидает жалобу
                                    // var forwardName_1 = api.Users.Get(new[] { (long)forwardId_1}, null, NameCase.Nom)[0].FirstName;
                                    // api.Messages.Send(new MessagesSendParams()
                                    // {

                                    //     PeerId = a.MessageNew.Message.PeerId,
                                    //     UserId = a.MessageNew.Message.UserId,
                                    //     ChatId = a.MessageNew.Message.ChatId,
                                    //     Message =  $"Пользователь [id{forwardId_1}|{forwardName_1}] подал жалобу на пользователя [id{forwardId}|{forwardName}].",
                                    //     // Forward = a.MessageNew.Message.AdminAuthorId. ,
                                    //     RandomId = getRandomMessageId()
                                    // }); ;
                                    break;
                                case 5:
                                    api.Messages.Send(new MessagesSendParams()
                                    {
                                        PeerId = a.MessageNew.Message.PeerId,
                                        UserId = a.MessageNew.Message.UserId,
                                        ChatId = a.MessageNew.Message.ChatId,
                                        Message = "Предложения в разработке",
                                        RandomId = getRandomMessageId()
                                    });
                                    break;
                                case 6:
                                    replyMessage = a.MessageNew.Message.ReplyMessage;
                                    sendMessageText = "Вы не указали, кому даёте -реп"; // сообщение, которое отправится пользователю (дефолтное значение - ошибка)

                                    forwardId = null;
                                    forwardName = "";
                                    LastName = "";
                                    User_Rep_ID = (long)forwardId;
                                    {
                                        // работа с БД
                                        // создаем подключение
                                        {
                                            using ApplicationContext db = new ApplicationContext();

                                            // получаем информацию о пользователе

                                            List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                                         where user.Id.Equals(User_Rep_ID)
                                                                         select user).ToList();      // ищем пользователя с таким ID

                                            if (FoundedUsers.Count > 0) // если пользотватель найден 
                                            {
                                                VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Прост овытаскиваем пользователя из массива
                                            }
                                            else    // если пользователя нет в базе данных
                                            {
                                                VkUser = new VkUser();      // создаем пользователя
                                                VkUser.Id = User_Rep_ID;    // задаем ему ID по ВК
                                                                            //VkUser.FirstName = forwardName;     // задаем имя
                                                                            //VkUser.LastName = LastName; // Задаем фамилимю 
                                                db.VkUsers.Add(VkUser);     // добавляем в базу данных
                                            }

                                            // добавляем репутацию
                                            VkUser.Affordable_reputation -= 1;

                                            // сохраняем в БД
                                            db.SaveChanges();
                                        }
                                    }
                                    if (replyMessage == null) // если нет ответа на сообщение, значит пытаемся вытащить через @
                                    {
                                        // ищем через регулярное выражение, кому отправляют +реп
                                        Regex regex = new Regex(@"[0-9]*\|");
                                        MatchCollection matches = regex.Matches(messageText);

                                        if (matches.Count > 0) // если в сообщении указан ID
                                        {
                                            Regex regex_1 = new Regex(@"club[0-9]*\|");
                                            MatchCollection match = regex_1.Matches(messageText);
                                            if (match.Count > 0)
                                            {
                                                sendMessageText = $"Выдавать и упоминать ботов - это неуважение к собеседникам &#128074;";
                                            }
                                            else
                                            {
                                                // на входе будет найдено что-то вида 122345|, убираем | и конвертируем в long
                                                forwardId = long.Parse(matches[0].Value.Replace("|", "")); // вытаскиваем ID того, кому даётся осуждение
                                                forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName;
                                                LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                            }
                                        }
                                    }
                                    else // если это ответ на какое-то сообщение
                                    {
                                        if (replyMessage.FromId < 0)
                                        {
                                            sendMessageText = $"Я всего лишь бот. Мне не нужно ваше осуждение";
                                        }
                                        else
                                        {
                                            forwardId = replyMessage.FromId; // вытаскиваем ID того, кому даётся осуждение
                                            forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем -реп
                                            LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                        }
                                    }
                                    if (forwardId != null && forwardId.Equals(a.MessageNew.Message.FromId))
                                    {
                                        Random light = new Random();
                                        int answer = light.Next(1, 4);
                                        switch (answer)
                                        {
                                            case 0:
                                                sendMessageText = $"&#128552; Что-то здесь не так, а вот что остается загадкой и для тебя, и для меня &#128521;";
                                                break;
                                            case 1:
                                                sendMessageText = $"&#128552; Вы пишете загадодками написано, пойду к Гадалке схожу, пусть она мне раскроет мне эту тайну &#128302;";
                                                break;
                                            case 2:
                                                sendMessageText = $"Полисмены проделали блестящую работу, уничтожив любые улики… " + "\r\n" + "Жаль, что я не шерлок Холмс, чтобы раскрыть ваш обман";
                                                break;
                                            case 3:
                                                sendMessageText = $"Нельзя опускать репутацию самому себе, у нас честные соревнования!";
                                                break;

                                        }
                                    }
                                    else if (forwardId != null)
                                    {
                                        //long User_Rep_ID = (long)forwardId;
                                        //if (!User_Data.ContainsKey(User_Rep_ID))
                                        //{
                                        //    User_Data[User_Rep_ID] = 0.0;
                                        //}
                                        //double User_Rep = User_Data[User_Rep_ID];
                                        //User_Rep -= 0.04;
                                        //User_Data[User_Rep_ID] = Math.Round(User_Rep, 3, MidpointRounding.AwayFromZero);

                                        User_Rep_ID = (long)forwardId;

                                        // работа с БД
                                        // создаем подключение
                                        using ApplicationContext db = new ApplicationContext();

                                        // получаем информацию о пользователе
                                        VkUser = null;
                                        List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                                     where user.Id.Equals(User_Rep_ID)
                                                                     select user).ToList();      // ищем пользователя с таким ID

                                        if (FoundedUsers.Count > 0) // если пользотватель найден 
                                        {
                                            VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Просто вытаскиваем пользователя из массива
                                        }
                                        else    // если пользователя нет в базе данных
                                        {
                                            VkUser = new VkUser();      // создаем пользователя
                                            VkUser.Id = User_Rep_ID;    // задаем ему ID по ВК
                                            //VkUser.FirstName = forwardName;     // задаем имя
                                            //VkUser.LastName = LastName; // Задаем фамилимю 
                                            db.VkUsers.Add(VkUser);     // добавляем в базу данных
                                        }

                                        // Отнимаем репутацию
                                        VkUser.Rating -= 0.04;
                                        VkUser.Rating = Math.Round(VkUser.Rating, 3);

                                        // сохраняем в БД
                                        db.SaveChanges();

                                        sendMessageText =
                                        $"&#128520;Осуждение оказано. Минус 0,04 отнимается от кармы пользователя [id{forwardId}|{forwardName} {LastName}]. Текущий рейтинг: {Math.Round(VkUser.Rating, 3, MidpointRounding.AwayFromZero)}";

                                        if (VkUser.Affordable_reputation <= 0)
                                        {
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"У вас закончилась доступная репутация &#128532;",
                                                RandomId = getRandomMessageId()

                                            });
                                        }
                                    }
                                    api.Messages.Send(new MessagesSendParams()
                                    {
                                        PeerId = a.MessageNew.Message.PeerId,
                                        UserId = a.MessageNew.Message.UserId,
                                        ChatId = a.MessageNew.Message.ChatId,
                                        Message = sendMessageText,
                                        RandomId = getRandomMessageId()

                                    });
                                    break;
                                case 7:
                                    if(a.MessageNew.Message.PeerId >= 2000000000)
                                    {
                                        forwardId = a.MessageNew.Message.FromId;
                                        forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName;
                                        LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                        User_Rep_ID = (long)forwardId;
                                        string Win = "Вы выжили!";
                                        string Lose = "Вы умерли!";
                                        Random Revolver = new Random();
                                        int value = Revolver.Next(0, 100);

                                        

                                        int chance = 65;    // шанс выигрыша 65%

                                        bool isWin = false; // выиграл или нет
                                        if (value > (100 - chance)) // если выиграл
                                        {
                                            isWin = true;
                                        }

                                        string ruletkaMessage;
                                        if (isWin)  // если выиграл
                                        {                                           
                                            // работа с БД
                                            // создаем подключение
                                            using ApplicationContext db = new ApplicationContext();

                                            // получаем информацию о пользователе
                                            VkUser = null;
                                            List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                                         where user.Id.Equals(User_Rep_ID)
                                                                         select user).ToList();      // ищем пользователя с таким ID

                                            if (FoundedUsers.Count > 0) // если пользотватель найден 
                                            {
                                                VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Просто вытаскиваем пользователя из массива
                                            }
                                            else    // если пользователя нет в базе данных
                                            {
                                                VkUser = new VkUser();      // создаем пользователя
                                                VkUser.Id = User_Rep_ID;    // задаем ему ID по ВК
                                                                            //VkUser.FirstName = forwardName;     // задаем имя
                                                                            //VkUser.LastName = LastName; //задаем Фамилию
                                                db.VkUsers.Add(VkUser);     // добавляем в базу данных
                                            }

                                            // Добавляем репутацию за рулетку
                                            VkUser.Rating += 5.01;
                                            VkUser.Rating = Math.Round(VkUser.Rating, 3);

                                            // сохраняем в БД
                                            db.SaveChanges();
                                            ruletkaMessage = $"&#129312; {Win} Плюс 5 к карме пользоваеля [id{forwardId}|{forwardName} {LastName}]. Текущий рейтинг: {Math.Round(VkUser.Rating, 3, MidpointRounding.AwayFromZero)}";
                                        }
                                        else // если проиграл
                                        {
                                            //forwardId = a.MessageNew.Message.FromId;
                                            //forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName;
                                            //LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                            //User_Rep_ID = (long)forwardId;
                                            // работа с БД
                                            // создаем подключение
                                            using ApplicationContext db = new ApplicationContext();

                                            // получаем информацию о пользователе
                                            VkUser = null;
                                            List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                                         where user.Id.Equals(User_Rep_ID)
                                                                         select user).ToList();      // ищем пользователя с таким ID

                                            if (FoundedUsers.Count > 0) // если пользотватель найден 
                                            {
                                                VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Просто вытаскиваем пользователя из массива
                                            }
                                            else    // если пользователя нет в базе данных
                                            {
                                                VkUser = new VkUser();      // создаем пользователя
                                                VkUser.Id = User_Rep_ID;    // задаем ему ID по ВК
                                                                            //VkUser.FirstName = forwardName;     // задаем имя
                                                                            //VkUser.LastName = LastName;     // задаем Фамилию
                                                db.VkUsers.Add(VkUser);     // добавляем в базу данных
                                            }

                                            // Отнимаем репутацию за рулетку
                                            VkUser.Rating -= 7.07;
                                            VkUser.Rating = Math.Round(VkUser.Rating, 3);

                                            // сохраняем в БД
                                            db.SaveChanges();

                                            ruletkaMessage = $"&#128128; {Lose} Минус 7 от кармы пользователя [id{forwardId}|{forwardName} {LastName}]. Текущий рейтинг: {Math.Round(VkUser.Rating, 3, MidpointRounding.AwayFromZero)}";
                                        }


                                        api.Messages.Send(new MessagesSendParams()
                                        {

                                            PeerId = a.MessageNew.Message.PeerId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = ruletkaMessage,
                                            RandomId = getRandomMessageId()
                                        });
                                    }
                                    
                                    break;

                                case 8: //предупреждение 
                                    try
                                    {
                                        replyMessage = a.MessageNew.Message.ReplyMessage;
                                        sendMessageText = "Вы не указали, на кого хотите выдать предуплеждение"; // сообщение, которое отправится пользователю (дефолтное значение - ошибка)

                                        forwardId = null;
                                        forwardName = "";
                                        LastName = "";
                                        forwardId_1 = a.MessageNew.Message.FromId; // вытаскиваем ID того, кто кидает жалобу
                                        forwardName_1 = api.Users.Get(new[] { (long)forwardId_1 }, null, NameCase.Nom)[0].FirstName;
                                        LastName_1 = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;

                                        if (replyMessage == null) // если нет ответа на сообщение, значит пытаемся вытащить через @
                                        {
                                            // ищем через регулярное выражение, кому отправляют жалобу
                                            Regex regex = new Regex(@"[0-9]*\|");
                                            MatchCollection matches = regex.Matches(messageText);

                                            if (matches.Count > 0) // если в сообщении указан ID
                                            {
                                                // на входе будет найдено что-то вида 122345|, убираем | и конвертируем в long
                                                forwardId = long.Parse(matches[0].Value.Replace("|", "")); // вытаскиваем ID того, кому даётся жалоба
                                                forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем жалобу
                                                LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                            }
                                        }
                                        else // если это ответ на какое-то сообщение
                                        {
                                            forwardId = replyMessage.FromId; // вытаскиваем ID того, кому даётся репутация
                                            forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем жалобу
                                            LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                        }


                                        if (forwardId != null && forwardId.Equals(a.MessageNew.Message.FromId))
                                        {
                                            Random light = new Random();
                                            int answer = light.Next(1, 5);
                                            switch (answer)
                                            {
                                                case 0:
                                                    sendMessageText = $"&#128552; Что-то здесь не так, а вот что остается загадкой и для тебя, и для меня &#128521;";
                                                    break;
                                                case 1:
                                                    sendMessageText = $"&#128552; Вы пишете загадодками написано, пойду к Гадалке схожу, пусть она мне раскроет мне эту тайну &#128302;";
                                                    break;
                                                case 2:
                                                    sendMessageText = $"Полисмены проделали блестящую работу, уничтожив любые улики… " + "\r\n" + "Жаль, что я не шерлок Холмс, чтобы раскрыть ваш обман";
                                                    break;
                                                case 3:
                                                    sendMessageText = $"Чем ты сам себе не угодил? Пойди и выспесь лучше)";
                                                    break;
                                                case 4:
                                                    sendMessageText = $"Кто я?! Пойди проветрись! Уж очень ты душный &#128514;";
                                                    break;

                                            }
                                        }
                                        else if (forwardId != null)
                                        {
                                            //    long User_Req_ID = (long)forwardId;
                                            //    if (!User_Data.ContainsKey(User_Req_ID))
                                            //    {
                                            //        User_Data[User_Req_ID] = 0.0;
                                            //    }
                                            //    double User_req = User_Data[User_Req_ID];
                                            //    User_req += 1;
                                            //    User_Data[User_Req_ID] = Math.Round(User_req, 3, MidpointRounding.AwayFromZero);
                                            sendMessageText =
                                            $"Пользователь [id{forwardId_1}|{forwardName_1} {LastName_1}] выдает предуплеждение пользователю [id{forwardId}|{forwardName} {LastName}]" + "\r\n" + $"Внимание! Если у вас будет 3 предупреждения и более, вы будете исключены из данной беседы!";

                                        }
                                        api.Groups.GetMembers(new GroupsGetMembersParams()
                                        {
                                            GroupId = Group_Id.ToString(),
                                            Count = 500,
                                            Offset = 0,
                                            //  Fields = UsersFields.All,
                                            Sort = GroupsSort.IdAsc,
                                            Filter = GroupsMemberFilters.Managers,
                                        });
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.PeerId,
                                            UserId = a.MessageNew.Message.AdminAuthorId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }
                                    catch
                                    {
                                        sendMessageText = $"У вас нет прав для использования этой команды";
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.PeerId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }

                                    break;
                                case 9: //бан
                                    try
                                    {
                                        replyMessage = a.MessageNew.Message.ReplyMessage;
                                        if (replyMessage == null)
                                        {

                                            Regex regex_ban = new Regex(@"[0-9]*\|");
                                            MatchCollection matches_ban = regex_ban.Matches(messageText);


                                            if (matches_ban.Count > 0) // если в сообщении указан ID
                                            {
                                                // на входе будет найдено что-то вида 122345|, убираем | и конвертируем в long
                                                forwardId = long.Parse(matches_ban[0].Value.Replace("|", "")); // вытаскиваем ID того, кому даётся осуждение
                                                forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName;
                                                LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;

                                                //long Chat = 0;
                                                //if (a.MessageNew.Message.ChatId != null)
                                                //{
                                                //    Chat = a.MessageNew.Message.ChatId.Value;
                                                //}
                                                ////var Chat = a.MessageNew.Message.ChatId;

                                                //api.Messages.RemoveChatUser(chatId: (ulong)Chat, userId: forwardId);

                                                User_Admin.Groups.BanUser(new GroupsBanUserParams()
                                                {
                                                    GroupId = Convert.ToInt64(Group_Id),
                                                    UserId = forwardId,
                                                    //EndDate = new DateTime(0, 0, 30, 0, 0, 0), //Данные указывающие год, месяц, день, час, минуту и секунду.
                                                    Reason = 0,
                                                    Comment = $"Тест работы функции",
                                                    CommentVisible = true,
                                                });
                                                sendMessageText = $"Пользователь [id{forwardId}|{forwardName} {LastName}] добавлен в черный список, доступ в беседы ему ограничен";
                                                api.Messages.Send(new MessagesSendParams()
                                                {
                                                    PeerId = a.MessageNew.Message.PeerId,
                                                    UserId = a.MessageNew.Message.UserId,
                                                    ChatId = a.MessageNew.Message.ChatId,
                                                    Message = sendMessageText,
                                                    RandomId = getRandomMessageId()
                                                });
                                            }

                                        }
                                        else
                                        {
                                            forwardId = replyMessage.FromId; // вытаскиваем ID того, кому даётся репутация
                                            forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем жалобу
                                            LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Nom)[0].LastName;
                                            User_Admin.Groups.BanUser(new GroupsBanUserParams()
                                            {
                                                GroupId = Convert.ToInt64(Group_Id),
                                                UserId = forwardId,
                                                //EndDate = new DateTime(0, 0, 30, 0, 0, 0), //Данные указывающие год, месяц, день, час, минуту и секунду.
                                                Reason = 0,
                                                Comment = $"Тест работы функции",
                                                CommentVisible = true,
                                            });
                                            sendMessageText = $"Пользователь [id{forwardId}|{forwardName} {LastName}] добавлен в черный список, доступ в беседы ему ограничен";

                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                RandomId = getRandomMessageId()
                                            });
                                        }

                                    }
                                    catch
                                    {
                                        sendMessageText = $"У вас нет прав для использования этой команды";
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.PeerId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }

                                    //ChatId = a.MessageNew.Message.ChatId,
                                    //UserId = a.MessageNew.Message.AdminAuthorId,
                                    //api.Messages.GetChatUsers(new MessagesGetParams()
                                    //{
                                    //    //PeerId = a.MessageNew.Message.PeerId,
                                    //    //UserId = a.MessageNew.Message.AdminAuthorId,
                                    //    //ChatId = a.MessageNew.Message.ChatId,
                                    //});                                //    {

                                    //        Message = "Баны пользователей будут позже доступны.",
                                    //        RandomId = new Random().Next(0, 100)
                                    //    });
                                    //Message replyMessage_1 = a.MessageNew.Message.ReplyMessage;

                                    //long? forwardId1 = replyMessage_1.FromId; // вытаскиваем ID того, кому даётся репутация
                                    //string forwardName1 = api.Users.Get(new[] { (long)forwardId1 }, null, NameCase.Nom)[0].FirstName; // получаем Имя того, кому даем реп

                                    //if ()
                                    //{
                                    //    api.Messages.Send(new MessagesSendParams()
                                    //    {
                                    //        PeerId = a.MessageNew.Message.PeerId,
                                    //        UserId = a.MessageNew.Message.AdminAuthorId,
                                    //        ChatId = a.MessageNew.Message.ChatId,
                                    //        Message = "Баны пользователей будут позже доступны.",
                                    //        RandomId = new Random().Next(0, 100)
                                    //    });
                                    //}
                                    //else
                                    //{
                                    //    return;

                                    //}

                                    //break;

                                    break;

                                case 10:
                                    {
                                        forwardId = a.MessageNew.Message.FromId; // вытаскиваем ID того, чей профиль хотим посмотреть
                                        forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].FirstName; // получаем Имя того, кому даем реп
                                        LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].LastName;
                                        using ApplicationContext db = new ApplicationContext();

                                        var currentUser = (from user in db.VkUsers
                                                           where user.Id == forwardId
                                                           select user).FirstOrDefault();

                                        string userText = currentUser == null
                                                          ? $"Пользователь с ID {forwardId} не найден."
                                                          : $" {currentUser.Rating} &#127942;" + "\r\n";


                                        sendMessageText = $"&#128123; Добро Пожаловать в магазин репутации!" + "\r\n" + "\r\n" +
                                            $"Здесь за определенную плату, я могу добавить еще попытки поднимать/опускать репутацию " + "\r\n" + "\r\n" +
                                            $"Ну что, готов обменять часть своей репутации? &#128302;" + "\r\n" + "\r\n" +
                                            $"В ответ отправь мне да или нет" + "\r\n" + "\r\n" +
                                            $"Курс обмена такой:" + "\r\n" + "\r\n" +
                                            $"10 XP в обмен на два доступных репа" + "\r\n" + "\r\n" +
                                            $"Ваша репутация: {userText}" + "\r\n" + "\r\n";
                                           
                                        api.Messages.Send(new MessagesSendParams()
                                        {

                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }
                                    break;
                                case 11://да
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        sendMessageText = "";
                                        forwardId = a.MessageNew.Message.FromId; // вытаскиваем ID того, чей профиль хотим посмотреть
                                        forwardName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].FirstName; // получаем Имя того, кому даем реп
                                        LastName = api.Users.Get(new[] { (long)forwardId }, null, NameCase.Gen)[0].LastName;
                                        using ApplicationContext db = new ApplicationContext();

                                        var currentUser = (from user in db.VkUsers
                                                           where user.Id == forwardId
                                                           select user).FirstOrDefault();
                                        forwardId = a.MessageNew.Message.FromId;


                                        User_Rep_ID = (long)forwardId;

                                        // получаем информацию о пользователе
                                        VkUser = null;
                                        List<VkUser> FoundedUsers = (from user in db.VkUsers
                                                                     where user.Id.Equals(User_Rep_ID)
                                                                     select user).ToList();      // ищем пользователя с таким ID

                                        if (FoundedUsers.Count > 0) // если пользотватель найден 
                                        {
                                            VkUser = FoundedUsers[0];   // то будет массив из 1 элемента. Просто вытаскиваем пользователя из массива
                                        }

                                        if (currentUser.Rating > 0 && currentUser.Rating % 10 > 0 && VkUser.Rating - currentUser.Rating > 0)
                                        {                                           
                                            VkUser.Rating -= 10;
                                            VkUser.Rating = Math.Round(VkUser.Rating, 3);
                                            VkUser.Affordable_reputation += 2;

                                            // сохраняем в БД
                                            db.SaveChanges();                                           
                                            sendMessageText = $"&#128123; С вами приятно иметь дело!" + "\r\n" + "\r\n" +
                                                        $"Ренесми добавила вам две дополнительные попытки" + "\r\n" + "\r\n" +
                                                        $"Остаток опыта: {currentUser.Rating}" + "\r\n" + "\r\n" +
                                                        $"Доступная репутация: {currentUser.Affordable_reputation}" + "\r\n" + "\r\n";
                                        }
                                        else
                                        {
                                            sendMessageText = $"К сожалению, у вас недостаточно опыта для оплаты!" + "\r\n" + "\r\n" +
                                                              $"&#128546;Приходи позже, когда у тебя будет больше опыта!" + "\r\n" + "\r\n";
                                        }
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }
                                    break;
                                case 12://нет
                                    if (a.MessageNew.Message.PeerId < 2000000000)
                                    {
                                        sendMessageText = $"Очень жаль!" + "\r\n" + "\r\n" +
                                               $"&#128546;Приходи позже!" + "\r\n" + "\r\n";


                                        api.Messages.Send(new MessagesSendParams()
                                        {

                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()
                                        });
                                    }
                                   
                                    break;
                                case 13:
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {

                                        sendMessageText = $"Пожалуйста, выберете интересующий вас раздел:";
                                        api.Messages.Send(new MessagesSendParams()
                                        {

                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Keyboard = keyboard,
                                            Message = sendMessageText,
                                            RandomId = getRandomMessageId()


                                        });
                                    }
                                    break;

                                case 14:
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        sendMessageText = $"Пристегните ремни, мы отправляемся в прошлое";
                                        using (Process process = new())
                                        {
                                            process.StartInfo.UseShellExecute = true;
                                            process.StartInfo.FileName = "https://www.mirea.ru/about/history-of-the-university/history-of-the-university/";
                                            process.Start();
                                        }
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            Keyboard = keyboard,
                                            RandomId = getRandomMessageId()
                                        });
                                    }                                   
                                    break;
                                case 15:
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        sendMessageText = $"Расписание доступно на сайте университета";
                                        using (Process process = new())
                                        {
                                            process.StartInfo.UseShellExecute = true;
                                            process.StartInfo.FileName = "https://www.mirea.ru/schedule";
                                            process.Start();
                                        }
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            Keyboard = keyboard,
                                            RandomId = getRandomMessageId()
                                        });
                                    }                                   
                                    break;
                                case 16:
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        sendMessageText = $"Карта кампусов будет позже доступна" + "\r\n" +
                                                                                "Примечание:" + "\r\n" +
                                                                                "В-78 - занятия в кампусе по адресу пр-т Вернадского, д.78" + "\r\n" +
                                                                                "В-86 - занятия в кампусе по адресу пр-т Вернадского, д.86" + "\r\n" +
                                                                                "МП-1 - занятия в кампусе по адресу ул. Малая Пироговская, д.1" + "\r\n" +
                                                                                "С-20 - занятия в кампусе по адресу ул. Стромынка, д.20" + "r\n" +
                                                                                "СГ-22 - занятия в кампусе по адресу 5-я ул. Соколиной горы, д.22" + "\r\n" +
                                                                                "" + "\r\n";
                                        var keyboard_1 = new KeyboardBuilder()
                                          .AddButton("Карта Кампуса В-78", "btnValue", KeyboardButtonColor.Primary)
                                          .SetInline(false)
                                          .SetOneTime()
                                          .AddLine()
                                          .AddButton("Карта Кампуса В-86", "btnValue", KeyboardButtonColor.Primary)
                                          .SetOneTime()
                                          .AddLine()
                                          .AddButton("Карта Кампуса МП-1", "btnValue", KeyboardButtonColor.Primary)
                                          .SetOneTime()
                                          .AddLine()
                                          .AddButton("Карта Кампуса 1-й Щипковский пер., д. 23", "btnValue", KeyboardButtonColor.Primary)
                                          .SetOneTime()
                                          .AddLine()
                                          .AddButton("Карта Кампуса С-20", "btnValue", KeyboardButtonColor.Primary)
                                          .SetOneTime()
                                          .AddLine()
                                          .AddButton("Карта Кампуса СГ-22", "btnValue", KeyboardButtonColor.Primary)
                                          .SetOneTime()
                                          .AddLine()
                                          .AddButton("Карта Кампуса ул. Усачева, д.7/1", "btnValue", KeyboardButtonColor.Primary)
                                          .SetOneTime()
                                          .AddLine()
                                          .AddButton("Вернуться Назад", "btnValue", KeyboardButtonColor.Negative)
                                          .Build();
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            Keyboard = keyboard_1,
                                            RandomId = getRandomMessageId()
                                        });
                                    }                                        
                                    break;
                                case 17:
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        sendMessageText = $"Пожалуйста, выберете интересующий вас раздел:";
                                        api.Messages.Send(new MessagesSendParams()
                                        {
                                            PeerId = a.MessageNew.Message.FromId,
                                            UserId = a.MessageNew.Message.UserId,
                                            ChatId = a.MessageNew.Message.ChatId,
                                            Message = sendMessageText,
                                            Keyboard = keyboard,
                                            RandomId = getRandomMessageId()
                                        });
                                    }

                                    break;
                                case 18: //карта В-78
                                         //var userId = 12345678; //Получатель сообщения
                                    var albumid = 288967919;
                                    var id = User_Admin.UserId.Value;
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        try
                                        {
                                            var photos = User_Admin.Photo.Get(new PhotoGetParams
                                            {
                                                AlbumId = PhotoAlbumType.Id(albumid),
                                                OwnerId = -215289543,

                                            });
                                            api.Messages.Send(new MessagesSendParams
                                            {
                                                Attachments = photos,
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"Карта кампуса Проспект Вернадского 78, Этот кампус очень интересен в плане строительства: Заходишь на территорию оказываешься на втором этаже, пройдешь по коридорам в институты окажешься на третьем этажа.",
                                                RandomId = getRandomMessageId(),
                                                Keyboard = keyboard,

                                            });
                                        }
                                        catch
                                        {
                                            sendMessageText = $"Ошибка в получении данных с сервера";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                Keyboard = keyboard,
                                                RandomId = getRandomMessageId()
                                            });
                                        }
                                    }                                                
                                    break;
                                case 19: //карта В-86 
                                         //var userId = 12345678; //Получатель сообщения
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        albumid = 288967919;
                                        id = User_Admin.UserId.Value;
                                        try
                                        {
                                            var photos = User_Admin.Photo.Get(new PhotoGetParams
                                            {
                                                AlbumId = PhotoAlbumType.Id(albumid),
                                                OwnerId = -215289543,

                                            });
                                            api.Messages.Send(new MessagesSendParams
                                            {
                                                //Attachments = photos,
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"Карта кампуса Проспект Вернадского 86, в разработке",
                                                RandomId = getRandomMessageId(),
                                                Keyboard = keyboard,

                                            });
                                        }
                                        catch
                                        {
                                            sendMessageText = $"Ошибка в получении данных с сервера";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                Keyboard = keyboard,
                                                RandomId = getRandomMessageId()
                                            });

                                        }
                                    }                              
                                    break;
                                case 20: //Карта Кампуса МП-1
                                         //var userId = 12345678; //Получатель сообщения
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        albumid = 288967919;
                                        id = User_Admin.UserId.Value;
                                        try
                                        {
                                            var photos = User_Admin.Photo.Get(new PhotoGetParams
                                            {
                                                AlbumId = PhotoAlbumType.Id(albumid),
                                                OwnerId = -215289543,

                                            });
                                            api.Messages.Send(new MessagesSendParams
                                            {
                                                //Attachments = photos,
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"Карта кампуса МП-1, в разработке",
                                                RandomId = getRandomMessageId(),
                                                Keyboard = keyboard,

                                            });
                                        }
                                        catch
                                        {
                                            sendMessageText = $"Ошибка в получении данных с сервера";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                Keyboard = keyboard,
                                                RandomId = getRandomMessageId()
                                            });
                                        }
                                    }                                    
                                    break;
                                case 21: //Карта Кампуса 1-й Щипковский пер., д. 23
                                         //var userId = 12345678; //Получатель сообщения
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        albumid = 288967919;
                                        id = User_Admin.UserId.Value;
                                        try
                                        {
                                            var photos = User_Admin.Photo.Get(new PhotoGetParams
                                            {
                                                AlbumId = PhotoAlbumType.Id(albumid),
                                                OwnerId = -215289543,

                                            });
                                            api.Messages.Send(new MessagesSendParams
                                            {
                                                //Attachments = photos,
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"Карта Кампуса 1-й Щипковский пер., д. 23 в разработке",
                                                RandomId = getRandomMessageId(),
                                                Keyboard = keyboard,

                                            });
                                        }
                                        catch
                                        {
                                            sendMessageText = $"Ошибка в получении данных с сервера";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                Keyboard = keyboard,
                                                RandomId = getRandomMessageId()
                                            });
                                        }
                                    }                                   
                                    break;
                                case 22: //Карта Кампуса С-20
                                         //var userId = 12345678; //Получатель сообщения
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        albumid = 288967919;
                                        id = User_Admin.UserId.Value;
                                        try
                                        {
                                            var photos = User_Admin.Photo.Get(new PhotoGetParams
                                            {
                                                AlbumId = PhotoAlbumType.Id(albumid),
                                                OwnerId = -215289543,

                                            });
                                            api.Messages.Send(new MessagesSendParams
                                            {
                                                //Attachments = photos,
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"Карта Кампуса С-20 в разработке",
                                                RandomId = getRandomMessageId(),
                                                Keyboard = keyboard,

                                            });
                                        }
                                        catch
                                        {
                                            sendMessageText = $"Ошибка в получении данных с сервера";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                Keyboard = keyboard,
                                                RandomId = getRandomMessageId()
                                            });
                                        }
                                    }
                                    break;
                                case 23: //Карта Кампуса СГ-22
                                         //var userId = 12345678; //Получатель сообщения

                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        albumid = 288967919;
                                        id = User_Admin.UserId.Value;
                                        try
                                        {
                                            var photos = User_Admin.Photo.Get(new PhotoGetParams
                                            {
                                                AlbumId = PhotoAlbumType.Id(albumid),
                                                OwnerId = -215289543,

                                            });
                                            api.Messages.Send(new MessagesSendParams
                                            {
                                                //Attachments = photos,
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"Карта Кампуса СГ-22 в разработке",
                                                RandomId = getRandomMessageId(),
                                                Keyboard = keyboard,

                                            });
                                        }
                                        catch
                                        {
                                            sendMessageText = $"Ошибка в получении данных с сервера";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                Keyboard = keyboard,
                                                RandomId = getRandomMessageId()
                                            });
                                        }
                                    }                                   
                                    break;
                                case 24: //Карта Кампуса ул. Усачева, д.7/1
                                         //var userId = 12345678; //Получатель сообщения
                                    if (a.MessageNew.Message.PeerId < 2000000000) // проверка на личное сообщение
                                    {
                                        albumid = 288967919;
                                        id = User_Admin.UserId.Value;
                                        try
                                        {
                                            var photos = User_Admin.Photo.Get(new PhotoGetParams
                                            {
                                                AlbumId = PhotoAlbumType.Id(albumid),
                                                OwnerId = -215289543,

                                            });
                                            api.Messages.Send(new MessagesSendParams
                                            {
                                                //Attachments = photos,
                                                PeerId = a.MessageNew.Message.PeerId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = $"Карта Кампуса ул. Усачева, д.7/1 в разработке",
                                                RandomId = getRandomMessageId(),
                                                Keyboard = keyboard,

                                            });
                                        }
                                        catch
                                        {
                                            sendMessageText = $"Ошибка в получении данных с сервера";
                                            api.Messages.Send(new MessagesSendParams()
                                            {
                                                PeerId = a.MessageNew.Message.FromId,
                                                UserId = a.MessageNew.Message.UserId,
                                                ChatId = a.MessageNew.Message.ChatId,
                                                Message = sendMessageText,
                                                Keyboard = keyboard,
                                                RandomId = getRandomMessageId()
                                            });
                                        }
                                    }
                                    break;

                                //api.Messages.Send(new MessagesSendParams()
                                //{
                                //    PeerId = a.MessageNew.Message.PeerId,
                                //    UserId = a.MessageNew.Message.UserId,
                                //    ChatId = a.MessageNew.Message.ChatId,
                                //    Message = sendMessageText,
                                //   // Keyboard = keyboard,
                                //    RandomId = getRandomMessageId()
                                //});
                                //break;
                                //api.Messages.GetByConversationMessageId (new GetByConversationMessageIdResult
                                //{

                                //});
                                //default:
                                //    sendMessageText = $"Ladies and Gentlemen, Boys and Girls, put your hands up together, the fun begins&#127881;&#127881;";
                                //    Random ID_sticker = new Random();
                                //    //Random ID_sticker_1 = new Random();
                                //    uint sticker = (uint)ID_sticker.Next(1, 49);
                                //    //uint sticker_1 = (uint) ID_sticker_1.Next(134, 165);
                                //    api.Messages.Send(new MessagesSendParams()
                                //    {
                                //        PeerId = a.MessageNew.Message.PeerId,
                                //        UserId = a.MessageNew.Message.UserId,
                                //        ChatId = a.MessageNew.Message.ChatId,
                                //        StickerId = sticker,
                                //        RandomId = getRandomMessageId()
                                //    });
                                //    api.Messages.Send(new MessagesSendParams()
                                //    {
                                //        PeerId = a.MessageNew.Message.PeerId,
                                //        UserId = a.MessageNew.Message.UserId,
                                //        ChatId = a.MessageNew.Message.ChatId,
                                //        Message = sendMessageText,
                                //        RandomId = getRandomMessageId()
                                //    });
                                //    //api.Messages.Send(new MessagesSendParams()
                                //    //{
                                //    //        PeerId = a.MessageNew.Message.PeerId,
                                //    //        UserId = a.MessageNew.Message.UserId,
                                //    //        ChatId = a.MessageNew.Message.ChatId,
                                //    //        StickerId = 163,//85 - сонный персик
                                //    //        RandomId = getRandomMessageId()
                                //    //});
                                //    break;

                            }
                            //Data.Dispose();
                            //Data.Close();
                        }
                    }

                    ts = poll.Ts;

                }
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine("Ошмбка в работе программы: " + e);
                Process.GetCurrentProcess().Kill();
            }
            

        }

        public static int getRandomMessageId()
        {
            int rand = lastRandomId;
            while (rand == lastRandomId)    // генерируем до тех пор, пока не будет отличное от предыдущего значение
            {
                rand = new Random().Next(0, 1000);
            }

            lastRandomId = rand;
            // Console.WriteLine("random: " + lastRandomId);
            return rand;
        }
        
    }
    
}
