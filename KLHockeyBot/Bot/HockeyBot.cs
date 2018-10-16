using System;
using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using KLHockeyBot.Configs;
using System.Linq;
using KLHockeyBot.Data;
using KLHockeyBot.DB;
using Telegram.Bot.Args;

namespace KLHockeyBot.Bot
{
    public static class HockeyBot
    {
        private static TelegramBotClient _bot;
        private static string _username;
        private static CommandProcessor _commands;
        private static DbCore _db;

        public static readonly List<Chat> Chats = new List<Chat>();

        public static bool End = true;

        public static void Start()
        {
            _bot = new TelegramBotClient(Config.BotToken);
            _db = new DbCore();
            _commands = new CommandProcessor(_bot, _db);

            var me = _bot.GetMeAsync().Result;
            Console.WriteLine("Hello my name is " + me.FirstName);
            Console.WriteLine("Username is " + me.Username);
            Console.WriteLine("Press ctrl+c to kill me.");

            _bot.OnMessage += Bot_OnMessage;
            _bot.OnCallbackQuery += Bot_OnCallbackQuery;
            _commands.OnAdminMessage += OnAdminCommandMessage;
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

        private static void OnAdminCommandMessage(object sender, AdminMessageEventArgs args)
        {
            var command = args.Command;
            var messageId = args.CurrentPoll.MessageId;
            var chatId = args.Chat.Id;

            if (command == "admin_init")
            {
                try
                {
                    _db.Disconnect();
                    DbCore.Initialization();
                    _db = new DbCore();
                    _commands = new CommandProcessor(_bot, _db);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unknown DBCore exception: " + e.Message + "\n" + e.InnerException);
                }
            }

            if (command == "admin_addvote" || command == "admin_deletevote" || command == "admin_deletepoll")
            {
                if (args.CurrentPoll == null) return;

                var chatByPoll = RestoreChatByPollMessageId(messageId, chatId);

                if (chatByPoll == null)
                {
                    Console.WriteLine("Cannot find chatFindedVoting for: " + messageId);
                }
                else
                {
                    var player = args.CurrentPlayer;
                    var vote = player != null
                        ? new Vote(args.CurrentPoll.MessageId, 0, "", player.Name, player.Surname, "Да")
                        : null;
                    var poll = chatByPoll.Polls.FindLast(x => x.MessageId == messageId);

                    switch (command)
                    {
                        case "admin_addvote":
                            if (poll == null || vote == null) break;
                            _commands.AddVoteToPoll(poll, vote);
                            _commands.RenderPoll(chatByPoll, messageId);
                            break;
                        case "admin_deletevote":
                            if (poll == null || vote == null) break;
                            _commands.DeleteVoteFromPoll(poll, vote);
                            _commands.RenderPoll(chatByPoll, messageId);
                            break;
                        case "admin_deletepoll":
                            var report = poll.Report;
                            report += "\n*Closed.*";
                            _commands.ClearPollVotes(poll);
                            _commands.DeletePoll(chatByPoll, poll);
                            _commands.RenderClosedPoll(chatByPoll, messageId, report);
                            break;
                    }
                }
            }
        }

        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var msg = e.Message.Text;
            var cid = e.Message.Chat.Id;
            var fromId = e.Message.From.Id;

            Console.WriteLine("Incoming request: " + msg);
            Console.WriteLine("Search known chat: " + e.Message.Chat.FirstName + "; " + cid);

            var restoredChat = RestoreChatById(cid);

            if (msg == null) return;

            msg = msg.Trim('/');
            msg = msg.Replace(_username, "");
            msg = msg.Replace("@", "");

            try
            {
                _commands.FindCommands(msg, restoredChat, fromId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown Commands.FindCommand exceprion: " + ex.Message);
            }
        }

        private static void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var messageId = e.CallbackQuery.Message.MessageId;
            var chatId = e.CallbackQuery.Message.Chat.Id;
            var fromId = e.CallbackQuery.From.Id;

            Console.WriteLine(
                $"Incoming callback from id:{e.CallbackQuery.From.Id} user:{e.CallbackQuery.From.Username} name:{e.CallbackQuery.From.FirstName} surname:{e.CallbackQuery.From.LastName}");
            if (e.CallbackQuery.Data.Contains('/'))
            {
                //it's command from help or admin
                var msg = e.CallbackQuery.Data.Trim('/');
                try
                {
                    var restoredChat = RestoreChatById(chatId);
                    _commands.FindCommands(msg, restoredChat, fromId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unknown Commands.FindCommand exception: " + ex.Message);
                }

                return;
            }

            var chatByPoll = RestoreChatByPollMessageId(messageId, chatId);

            if (chatByPoll == null)
            {
                Console.WriteLine("Cannot find chatFindedVoting for: " + messageId);
            }
            else
            {
                if (e.CallbackQuery.Data != "Show")
                {
                    _commands.UpdatePoll(chatByPoll, messageId, e.CallbackQuery);
                }

                _commands.RenderPoll(chatByPoll, messageId);
            }
        }

        private static Chat RestoreChatByPollMessageId(int messageId, long chatId)
        {
            var chat = Chats.FindLast(c=> c.Polls.Any(poll => poll.MessageId == messageId));
            if (chat == null)
            {
                chat = RestoreChatById(chatId);
                RestorePollFromDb(messageId, chat);
            }

            return chat;
        }

        private static Chat RestoreChatById(long chatId)
        {
            var chat = Chats.FindLast(c => c.Id == chatId);
            if (chat == null)
            {
                chat = new Chat(chatId);
                Chats.Add(chat);
            }

            return chat;
        }

        private static void RestorePollFromDb(int messageId, Chat chat)
        {
            var poll = _db.GetPollByMessageId(messageId);
            if (poll == null) return;

            chat.Polls.Add(poll);

            poll.Votes = _db.GetVotesByMessageId(messageId);
            Console.WriteLine("Poll restored from DB: " + poll.Question);
        }
    }
}
