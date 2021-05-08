using System;
using System.Collections.Generic;
using System.Threading;
using Telegram.Bot;
using KLHockeyBot.Configs;
using System.Linq;
using KLHockeyBot.Entities;
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

        public static readonly List<HockeyChat> Chats = new List<HockeyChat>();

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

            _bot.OnUpdate += Bot_OnUpdate;
            _bot.OnMessage += Bot_OnMessage;
            _bot.OnCallbackQuery += Bot_OnCallbackQuery;
            _commands.OnAdminMessage += OnAdminCommandMessage;
            _commands.OnPollMessage += OnPollCommandMessage;
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
        private static async void Bot_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            switch(e.Update.Type)
            {
                case Telegram.Bot.Types.Enums.UpdateType.PreCheckoutQuery:
                    try
                    {
                        await _bot.AnswerPreCheckoutQueryAsync(e.Update.PreCheckoutQuery.Id);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                break;
            }
        }
        private static void OnPollCommandMessage(object sender, PollMessageEventArgs args)
        {
            var command = args.Cmd;
            var arg = args.Arg;
            var messageId = args.ReplyId;
            var chatId = args.Chat.Id;

            var chatByPoll = RestoreChatByPollMessageId(messageId, chatId);

            if (chatByPoll == null)
            {
                Console.WriteLine("Cannot find chatFindedVoting for: " + messageId);
            }
            else
            {
                var vote = new Vote(args.ReplyId, 0, "", "+" + arg, "", "Да");
                var poll = chatByPoll.Polls.FindLast(x => x.MessageId == messageId);

                switch (command)
                {
                    case "add":
                        if (poll == null || vote == null) break;
                        _commands.AddVoteToPoll(poll, vote);
                        _commands.RenderPoll(chatByPoll, messageId);
                        break;
                    case "del":
                        if (poll == null || vote == null) break;
                        _commands.DeleteVoteFromPoll(poll, vote);
                        _commands.RenderPoll(chatByPoll, messageId);
                        break;
                }
            }
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
        }
        private static void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var msg = e.Message.Text;
            var cid = e.Message.Chat.Id;
            Console.WriteLine("Incoming request: " + msg);

            if(e.Message.Type == Telegram.Bot.Types.Enums.MessageType.SuccessfulPayment)
            {
                var payment = e.Message.SuccessfulPayment;
                Console.WriteLine("SuccessfulPayment: " + payment.InvoicePayload);
                var paymentBoardChatId = (long)0;
                var paymentBoardMsgId = 0;
                var splittedPayload = payment.InvoicePayload.Split(';');
                if(splittedPayload.Length != 2) 
                {
                    Console.WriteLine("Wrong payment.InvoicePayload to update payment board");
                    return;
                }
                Int64.TryParse(splittedPayload[0], out paymentBoardChatId);
                Int32.TryParse(splittedPayload[1], out paymentBoardMsgId);
                var chat = RestoreChatById(paymentBoardChatId);
                UpdatePaymentBoard(chat, paymentBoardMsgId, e.Message.From, payment.TotalAmount, $"Оплата по {payment.TotalAmount/100}");
                return;
            }

            Console.WriteLine("Search known chat: " + e.Message.Chat.FirstName + "; " + cid);
            var replyId = e.Message.ReplyToMessage?.MessageId;
            var restoredChat = RestoreChatById(cid);
            if (msg == null) return;
            msg = msg.Trim('/');
            msg = msg.Replace(_username, "");
            msg = msg.Replace("@", "");
            try
            {
                _commands.ParseCommand(msg, restoredChat, replyId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unknown Commands.FindCommand exceprion: " + ex.Message);
            }
        }

        private static async void UpdatePaymentBoard(HockeyChat chat, int paymentBoardMsgId, Telegram.Bot.Types.User user, int totalAmount, string OrderName)
        {
            if (chat.Payments == null) chat.Payments = new List<HockeyPayment>();
            var payment = chat.Payments.FindLast(x => x.MessageId == paymentBoardMsgId);
            if (payment == null) 
            {
                payment = new HockeyPayment() {MessageId = paymentBoardMsgId, Payers = new List<Payer>(), Name = OrderName};
                chat.Payments.Add(payment);          
            }

            var payer = new Payer(paymentBoardMsgId, user.Id, user.Username, user.FirstName, user.LastName, totalAmount);
            payment.Payers.Add(payer);

            try
            {
                await _bot.EditMessageTextAsync(chat.Id, payment.MessageId, payment.Report, parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void Bot_OnCallbackQuery(object sender, CallbackQueryEventArgs e)
        {
            var messageId = e.CallbackQuery.Message.MessageId;
            var chatId = e.CallbackQuery.Message.Chat.Id;
            var replyId = e.CallbackQuery.Message.ReplyToMessage?.MessageId;

            Console.WriteLine(
                $"Incoming callback from id:{e.CallbackQuery.From.Id} user:{e.CallbackQuery.From.Username} name:{e.CallbackQuery.From.FirstName} surname:{e.CallbackQuery.From.LastName}");
            if (e.CallbackQuery.Data.Contains('/'))
            {
                //it's command from help or admin
                var msg = e.CallbackQuery.Data.Trim('/');
                try
                {
                    var restoredChat = RestoreChatById(chatId);
                    _commands.ParseCommand(msg, restoredChat, replyId);
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

        private static HockeyChat RestoreChatByPollMessageId(int messageId, long chatId)
        {
            var chat = Chats.FindLast(c=> c.Polls.Any(poll => poll.MessageId == messageId));
            if (chat == null)
            {
                chat = RestoreChatById(chatId);
                RestorePollFromDb(messageId, chat);
            }

            return chat;
        }
        private static HockeyChat RestorePaymentByMessageId(int messageId, long chatId)
        {
            var chat = Chats.FindLast(c=> c.Payments.Any(p => p.MessageId == messageId));
            if (chat == null)
            {
                chat = RestoreChatById(chatId);
                RestorePaymentFromDb(messageId, chat);
            }

            return chat;
        }

        private static HockeyChat RestoreChatById(long chatId)
        {
            var chat = Chats.FindLast(c => c.Id == chatId);
            if (chat == null)
            {
                chat = new HockeyChat(chatId);
                Chats.Add(chat);
            }

            return chat;
        }

        private static void RestorePollFromDb(int messageId, HockeyChat chat)
        {
            var poll = _db.GetPollByMessageId(messageId);
            if (poll == null) return;

            chat.Polls.Add(poll);

            poll.Votes = _db.GetVotesByMessageId(messageId);
            Console.WriteLine("Poll restored from DB: " + poll.Question);
        }

        private static void RestorePaymentFromDb(int messageId, HockeyChat chat)
        {
            var payment = _db.GetPaymentByMessageId(messageId);
            if (payment == null) return;

            chat.Payments.Add(payment);

            payment.Payers = _db.GetPayersByMessageId(messageId);
            Console.WriteLine("Payment restored from DB: " + payment.Name);
        }
    }
}
