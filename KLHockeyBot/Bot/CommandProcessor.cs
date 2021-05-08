using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KLHockeyBot.Configs;
using KLHockeyBot.Entities;
using KLHockeyBot.DB;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace KLHockeyBot.Bot
{
    public class CommandProcessor
    {
        private readonly TelegramBotClient _bot;
        private readonly DbCore _db;
        private HockeyPoll _currentPoll;

        public CommandProcessor(TelegramBotClient bot, DbCore db)
        {
            _bot = bot;
            _db = db;
        }

        public event EventHandler<AdminMessageEventArgs> OnAdminMessage;
        public event EventHandler<PollMessageEventArgs> OnPollMessage;

        public void ParseCommand(string msg, HockeyChat chat, int? replyId)
        {
            var msgSplitted = msg.Split(' ');
            if (msgSplitted.Length < 2)
            {
                return;
            }
            var cmd = msgSplitted.First().ToLower();
            var arg = msg.Substring(cmd.Length + 1);
            chat.CommandsQueue.Enqueue(new Command() { Cmd = cmd, Arg = arg });
            ProcessCommands(chat, replyId);            
        }

        private void ProcessCommands(HockeyChat chat, int? replyId)
        {
            var commands = chat.CommandsQueue;
            while (commands.Count > 0)
            {
                var command = commands.Dequeue();

                if (command.Cmd == "vote")
                {
                    AddPoll(chat, command.Arg);
                    continue;
                }

                if (command.Cmd == "pay")
                {
                    AddPay(chat, command.Arg);
                    continue;
                }

                if(command.Cmd == "add" || command.Cmd == "del")
                {
                    OnPollMessage.Invoke(this,
                        new PollMessageEventArgs(command.Cmd, command.Arg, chat, replyId == null ? 0 : (int)replyId));
                    continue;
                }
            }
        }

        private void DumpPlayers()
        {
            var players = _db.GetAllPlayers();
            var dumpTxt = "";
            var dumpFileName = $"database_players_dump_{DateTime.Now.Day}{DateTime.Now.Month}{DateTime.Now.Year}_{DateTime.Now.Hour}.txt";

            foreach (var player in players)
            {
                dumpTxt += $"{player.Number};{player.Surname};{player.Name};{player.SecondName};{player.Birthday};{player.Position};{player.Status};{player.TelegramUserid}\n";
            }

            try
            {
                using (var stream = File.CreateText(Path.Combine(Config.DbDirPath, dumpFileName)))
                {
                    stream.Write(dumpTxt);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        internal void UpdatePoll(HockeyChat chat, int msgid, CallbackQuery e)
        {
            var poll = chat.Polls.FindLast(x => x.MessageId == msgid);
            if (poll == null) return;
            _currentPoll = poll;

            var user = e.From;
            var player = _db.GetPlayerByUserid(user.Id);
            var vote = new Vote(msgid, user.Id, user.Username, player == null ? user.FirstName : player.Name,
                player == null ? user.LastName : player.Surname, e.Data);
            var voteDupl = poll.Votes.FindLast(x => x.TelegramUserId == vote.TelegramUserId);
            if (voteDupl != null)
            {
                if (voteDupl.Data == vote.Data) return;

                voteDupl.Data = vote.Data;
                _db.UpdateVoteData(msgid, vote.TelegramUserId, vote.Data);
            }
            else
            {
                AddVoteToPoll(poll, vote);
            }
        }

        internal void AddVoteToPoll(HockeyPoll poll, Vote vote)
        {
            poll.Votes.Add(vote);
            _db.AddVote(vote);
        }

        internal void DeleteVoteFromPoll(HockeyPoll poll, Vote vote)
        {
            poll.Votes.RemoveAll(v => v.MessageId == vote.MessageId && 
                                      v.Name == vote.Name && 
                                      v.TelegramUserId == vote.TelegramUserId && 
                                      v.Data == vote.Data && 
                                      v.Surname == vote.Surname &&
                                      v.Username == vote.Username);
            _db.DeleteVote(vote);
        }

        internal async void RenderPoll(HockeyChat chat, int messageId)
        {
            var poll = chat.Polls.FindLast(x => x.MessageId == messageId);
            if (poll == null) return;
            _currentPoll = poll;

            var noCnt = poll.Votes.Count(x => x.Data == "Не");
            var yesCnt = poll.Votes.Count(x => x.Data == "Да");

            try
            {
                await _bot.EditMessageTextAsync(chat.Id, messageId, poll.Report, parseMode: ParseMode.Markdown,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new InlineKeyboardButton
                        {
                            Text = $"Да – {yesCnt}",
                            CallbackData = "Да"
                        },
                        new InlineKeyboardButton
                        {
                            Text = $"Не – {noCnt}",
                            CallbackData = "Не"
                        }
                    }));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        private async void AddPay(HockeyChat chat, string arg)
        {                       
            var msg = await _bot.SendTextMessageAsync(chat.Id, $"{arg}");
            var payment = new HockeyPayment() {MessageId = msg.MessageId, Payers = new List<Payer>(), Name = arg};
            var payload = $"{chat.Id};{msg.MessageId}";

            var prices = new List<Telegram.Bot.Types.Payments.LabeledPrice>() { new Telegram.Bot.Types.Payments.LabeledPrice("Traktorista", 30000) };
            await _bot.SendInvoiceAsync(chat.Id, arg, "Да не обеднеет рука дающего", payload, Config.UkassaToken, "sasality", "RUB", prices);

            chat.Payments.Add(payment);
        }
        private async void AddPoll(HockeyChat chat, string arg)
        {
            chat.VoteMode = false;
            var btnYes = new InlineKeyboardButton
            {
                Text = "Да",
                CallbackData = "Да"
            };
            var btnNo = new InlineKeyboardButton
            {
                Text = "Не",
                CallbackData = "Не"
            };
            var btnShow = new InlineKeyboardButton
            {
                Text = "Показать результаты",
                CallbackData = "Show"
            };
            var keyboard = new InlineKeyboardMarkup(new[] { new [] { btnYes, btnNo }, new[] { btnShow } } );

            var msg = await _bot.SendTextMessageAsync(chat.Id, $"{arg}", replyMarkup: keyboard);
            var v = new List<Vote>();
            var poll = new HockeyPoll() {MessageId = msg.MessageId, Votes = v, Question = arg};
            _currentPoll = poll;

            _db.AddPoll(poll);
            var addedPoll = _db.GetPollByMessageId(poll.MessageId);
            poll.Id = addedPoll.Id;

            chat.Polls.Add(poll);
        }
    }
}
