using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using KLHockeyBot.Entities;
using KLHockeyBot.Database;

namespace KLHockeyBot.Services;

public class CommandProcessor(ITelegramBotClient bot, BotDatabase db)
{
    public event EventHandler<PollMessageEventArgs> OnPollMessage;

    public async Task ParseCommandAsync(string msg, HockeyChat chat, int? replyId)
    {
        var msgSplitted = msg.Split(' ');
        if (msgSplitted.Length < 2)
        {
            return;
        }
        var cmd = msgSplitted.First().ToLower();
        var arg = msg[(cmd.Length + 1)..];
        chat.CommandsQueue.Enqueue(new Command() { Cmd = cmd, Arg = arg });
        await ProcessCommands(chat, replyId);
    }

    private async Task ProcessCommands(HockeyChat chat, int? replyId)
    {
        var commands = chat.CommandsQueue;
        while (commands.Count > 0)
        {
            var command = commands.Dequeue();

            switch (command.Cmd)
            {
                case "vote":
                    await AddPollAsync(chat, command.Arg);
                    continue;
                case "add" or "del":
                    OnPollMessage?.Invoke(this,
                        new PollMessageEventArgs(command.Cmd, command.Arg, chat, replyId ?? 0));
                    continue;
                case "money":
                    {
                        var n = 0;
                        try
                        {
                            n = Convert.ToInt32(command.Arg);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        await RenderMoneyAsync(chat, n);
                        break;
                    }
            }
        }
    }

    public bool UpdatePoll(HockeyChat chat, int msgId, CallbackQuery e)
    {
        var poll = chat.Polls.FindLast(x => x.MessageId == msgId);
        if (poll == null) return false;

        var user = e.From;
        var player = db.GetPlayerByUserid(user.Id);
        var vote = new Vote(msgId, user.Id, user.Username, player == null ? user.FirstName : player.Name,
            player == null ? user.LastName : player.Surname, e.Data);
        var voteDuplicate = poll.Votes.FindLast(x => x.TelegramUserId == vote.TelegramUserId);
        if (voteDuplicate != null)
        {
            if (voteDuplicate.Data == vote.Data) return false;

            voteDuplicate.Data = vote.Data;
            db.UpdateVoteData(msgId, vote.TelegramUserId, vote.Data);
        }
        else
        {
            AddVoteToPoll(poll, vote);
        }
        return true;
    }

    public void AddVoteToPoll(HockeyPoll poll, Vote vote)
    {
        poll.Votes.Add(vote);
        db.AddVote(vote);
    }

    public void DeleteVoteFromPoll(HockeyPoll poll, Vote vote)
    {
        poll.Votes.RemoveAll(v => v.MessageId == vote.MessageId &&
                                  v.Name == vote.Name &&
                                  v.TelegramUserId == vote.TelegramUserId &&
                                  v.Data == vote.Data &&
                                  v.Surname == vote.Surname &&
                                  v.Username == vote.Username);
        db.DeleteVote(vote);
    }

    public async Task RenderPollAsync(HockeyChat chat, int messageId)
    {
        var poll = chat.Polls.FindLast(x => x.MessageId == messageId);
        if (poll == null) return;

        var noCnt = poll.Votes.Count(x => x.Data == "Не");
        var yesCnt = poll.Votes.Count(x => x.Data == "Да");

        try
        {
            await bot.EditMessageTextAsync(chat.Id, messageId, poll.Report, parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(
                    new InlineKeyboardButton($"Да – {yesCnt}") { CallbackData = "Да" },
                    new InlineKeyboardButton($"Не – {noCnt}") { CallbackData = "Не" }
                ));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public async Task RenderMoneyAsync(HockeyChat chat, int n)
    {
        var messageText = db.GetLastEventDetails(n);
        try
        {
            await bot.SendTextMessageAsync(chat.Id, $"{messageText}", null, ParseMode.MarkdownV2);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task AddPollAsync(HockeyChat chat, string arg)
    {
        chat.VoteMode = false;
        var btnYes = new InlineKeyboardButton("Да") { CallbackData = "Да" };
        var btnNo = new InlineKeyboardButton("Не") { CallbackData = "Не" };
        var btnShow = new InlineKeyboardButton("Показать результаты") { CallbackData = "Show" };
        var keyboard = new InlineKeyboardMarkup([[btnYes, btnNo], [btnShow]]);

        var msg = await bot.SendTextMessageAsync(chat.Id, $"{arg}", replyMarkup: keyboard);
        var v = new List<Vote>();
        var poll = new HockeyPoll() { MessageId = msg.MessageId, Votes = v, Question = arg };

        db.AddPoll(poll);
        var addedPoll = db.GetPollByMessageId(poll.MessageId);
        poll.Id = addedPoll.Id;

        chat.Polls.Add(poll);
    }
}