using System;
using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using KLHockeyBot.Configs;
using System.Linq;
using KLHockeyBot.Data;
using Telegram.Bot.Args;

namespace KLHockeyBot.Bot
{
    public static class HockeyBot
    {
        private static TelegramBotClient _bot;
        private static string _username;
        private static CommandProcessor _commands;

        public static readonly List<Player> Players = new List<Player>();
        public static readonly List<Chat> Chats = new List<Chat>();

        public static bool End = true;
        public static void Start()
        {
            //var webProxy = new WebProxy("proxy.my.ru", 3128);
            //webProxy.Credentials = new NetworkCredential(@"login", @"XXX");
            //Bot = new TelegramBotClient(Config.BotToken, webProxy);

            _bot = new TelegramBotClient(Config.BotToken);

            _commands = new CommandProcessor(_bot);

            var me = _bot.GetMeAsync().Result;
            Console.WriteLine("Hello my name is " + me.FirstName);
            Console.WriteLine("Username is " + me.Username);
            Console.WriteLine("Press ctrl+c to kill me.");

            _bot.OnMessage += Bot_OnMessage;
            _bot.OnCallbackQuery += Bot_OnCallbackQuery;
            _username = me.Username;

            Console.WriteLine("StartReceiving...");
            _bot.StartReceiving();

            while (End)
            {
                //Nothing to do, just sleep 1 sec
                //ctrl+c break cycle
                Thread.Sleep(1000);
            }

            Console.WriteLine("StopReceiving...");
            _bot.StopReceiving();
        }

        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var msg = e.Message.Text;
            var cid = e.Message.Chat.Id;
            var fromId = e.Message.From.Id;

            Console.WriteLine("Incoming request: " + msg);
            Console.WriteLine("Search known chat: " + e.Message.Chat.FirstName + "; " + cid);

            var chatFinded = Chats.FindLast(chat => chat.Id == cid);
            if (chatFinded == null)
            {
                chatFinded = new Chat(cid);
                Chats.Add(chatFinded);
            }

            if (msg == null) return;

            msg = msg.Trim('/');
            msg = msg.Replace(_username, "");
            msg = msg.Replace("@","");

            try
            {
                _commands.FindCommands(msg, chatFinded, fromId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown Commands.FindCommand exceprion: " + ex.Message);
            }
        }

        private static void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            Console.WriteLine($"Incoming callback from id:{e.CallbackQuery.From.Id} user:{e.CallbackQuery.From.Username} name:{e.CallbackQuery.From.FirstName} surname:{e.CallbackQuery.From.LastName}");
            if (e.CallbackQuery.Data.Contains('/'))
            {
                //it's command from help or admin
                var msg = e.CallbackQuery.Data.Trim('/');
                try
                {
                    _commands.FindCommands(msg, new Chat(e.CallbackQuery.Message.Chat.Id), e.CallbackQuery.Message.From.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unknown Commands.FindCommand exceprion: " + ex.Message);
                }

                return;
            }

            var chatFindedVote = Chats.FindLast(chat => chat.WaitingVotings.Any(voting => voting.MessageId == e.CallbackQuery.Message.MessageId));
            if (chatFindedVote == null)
            {
                var restoredChat = Chats.FindLast(chat => chat.Id == e.CallbackQuery.Message.Chat.Id);
                if (restoredChat == null)
                {
                    restoredChat = new Chat(e.CallbackQuery.Message.Chat.Id);
                    Chats.Add(restoredChat);
                }
                //try to restore from db
                _commands.TryToRestoreVotingFromDb(e.CallbackQuery.Message.MessageId, restoredChat);
                chatFindedVote = Chats.FindLast(chat => chat.WaitingVotings.Any(voting => voting.MessageId == e.CallbackQuery.Message.MessageId));
            }

            if (chatFindedVote == null)
            {
                Console.WriteLine("Cannot find chatFindedVoting for: " + e.CallbackQuery.Message.MessageId);
            }
            else
            {
                _commands.ContinueWaitingVoting(chatFindedVote, e.CallbackQuery.Message.MessageId, e.CallbackQuery, e.CallbackQuery.Data=="Show" ? true : false);
            }
        }
    }
}
