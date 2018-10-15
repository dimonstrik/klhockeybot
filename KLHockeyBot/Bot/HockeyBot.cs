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

        public static readonly List<Chat> Chats = new List<Chat>();

        public static bool End = true;
        public static void Start()
        {
            _bot = new TelegramBotClient(Config.BotToken);

            _commands = new CommandProcessor(_bot);

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

            if (command == "admin_addvote" || command == "admin_deletevote" || command == "admin_deletepoll")
            { 
                    var chatByPoll = Chats.FindLast(chat => chat.Polls.Any(poll => poll.MessageId == args.CurrentPoll.MessageId));
                    if (chatByPoll == null)
                    {
                        var restoredChat = Chats.FindLast(chat => chat.Id == args.Chat.Id);
                        if (restoredChat == null)
                        {
                            restoredChat = new Chat(args.Chat.Id);
                            Chats.Add(restoredChat);
                        }
                        //try to restore from db
                        _commands.TryToRestorePollFromDb(args.CurrentPoll.MessageId, restoredChat);
                        chatByPoll = Chats.FindLast(chat => chat.Polls.Any(poll => poll.MessageId == args.CurrentPoll.MessageId));
                    }

                    if (chatByPoll == null)
                    {
                        Console.WriteLine("Cannot find chatFindedVoting for: " + args.CurrentPoll.MessageId);
                    }
                    else
                    {
                        var player = args.CurrentPlayer;
                        !!!check for null
                        var vote = new Vote(args.CurrentPoll.MessageId, 0, "", player.Name, player.Surname, "Да");
                        var poll = chatByPoll.Polls.FindLast(x => x.MessageId == args.CurrentPoll.MessageId);

                        switch (command)
                        {
                            case "admin_addvote":
                                _commands.AddVoteToPoll(poll, vote);
                                _commands.RenderPoll(chatByPoll, args.CurrentPoll.MessageId);
                                break;
                            case "admin_deletevote":
                                _commands.DeleteVoteFromPoll(poll, vote);
                                _commands.RenderPoll(chatByPoll, args.CurrentPoll.MessageId);
                                break;
                            case "admin_deletepoll":
                                var report = _commands.GetPollReport(poll);
                                report += "\n*Closed.*";
                                foreach (var pollVote in poll.Votes)
                                {
                                    _commands.DeleteVoteFromPoll(poll, pollVote);
                                }
                                _commands.DeletePoll(chatByPoll, poll);
                                _commands.RenderClosedPoll(chatByPoll, args.CurrentPoll.MessageId, report);
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
                    Console.WriteLine("Unknown Commands.FindCommand exception: " + ex.Message);
                }

                return;
            }

            var chatByPoll = Chats.FindLast(chat => chat.Polls.Any(voting => voting.MessageId == e.CallbackQuery.Message.MessageId));
            if (chatByPoll == null)
            {
                var restoredChat = Chats.FindLast(chat => chat.Id == e.CallbackQuery.Message.Chat.Id);
                if (restoredChat == null)
                {
                    restoredChat = new Chat(e.CallbackQuery.Message.Chat.Id);
                    Chats.Add(restoredChat);
                }
                //try to restore from db
                _commands.TryToRestorePollFromDb(e.CallbackQuery.Message.MessageId, restoredChat);
                chatByPoll = Chats.FindLast(chat => chat.Polls.Any(voting => voting.MessageId == e.CallbackQuery.Message.MessageId));
            }

            if (chatByPoll == null)
            {
                Console.WriteLine("Cannot find chatFindedVoting for: " + e.CallbackQuery.Message.MessageId);
            }
            else
            {
                if (e.CallbackQuery.Data != "Show")
                {
                    _commands.UpdatePoll(chatByPoll, e.CallbackQuery.Message.MessageId, e.CallbackQuery);
                }
                _commands.RenderPoll(chatByPoll, e.CallbackQuery.Message.MessageId);
            }
        }
    }
}
