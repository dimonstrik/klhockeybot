using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using KLHockeyBot.Entities;
using KLHockeyBot.Database;

namespace KLHockeyBot.Services;

public class UpdateHandler : IUpdateHandler
{
    public readonly List<HockeyChat> Chats = [];
    private readonly BotDatabase _db;
    private readonly CommandProcessor _commands;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(BotDatabase db, CommandProcessor commands, ILogger<UpdateHandler> logger)
    {
        _db = db;
        _commands = commands;
        _logger = logger;

        _commands.OnPollMessage += async delegate (object _, PollMessageEventArgs args)
        {
            await OnPollCommandMessage(args);
        };
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await (update switch
        {
            { Message: { } message } => OnMessage(message),
            { EditedMessage: { } message } => OnMessage(message),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(callbackQuery),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("HandleError: {Exception}", exception);
        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    private async Task OnPollCommandMessage(PollMessageEventArgs args)
    {
        var command = args.Cmd;
        var arg = args.Arg;
        var messageId = args.ReplyId;
        var chatId = args.Chat.Id;

        var chatByPoll = RestoreChatByPollMessageId(messageId, chatId);

        if (chatByPoll == null)
        {
            Console.WriteLine($"Cannot restore chat by {messageId} message");
        }
        else
        {
            var vote = new Vote(args.ReplyId, 0, "", "+" + arg, "", "Да");
            var poll = chatByPoll.Polls.FindLast(x => x.MessageId == messageId);

            switch (command)
            {
                case "add":
                    if (poll == null) break;
                    _commands.AddVoteToPoll(poll, vote);
                    await _commands.RenderPollAsync(chatByPoll, messageId);
                    break;
                case "del":
                    if (poll == null) break;
                    _commands.DeleteVoteFromPoll(poll, vote);
                    await _commands.RenderPollAsync(chatByPoll, messageId);
                    break;
            }
        }
    }

    private async Task OnMessage(Message message)
    {
        var text = message.Text;
        var cid = message.Chat.Id;
        Console.WriteLine("Incoming request: " + text);
        Console.WriteLine("Search known chat: " + message.Chat.FirstName + "; " + cid);
        var replyId = message.ReplyToMessage?.MessageId;
        var restoredChat = RestoreChatById(cid);
        if (text == null) return;
        text = text.Trim('/');
        text = text.Replace("@", "");
        try
        {
            await _commands.ParseCommandAsync(text, restoredChat, replyId);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unknown Commands.FindCommand exception: " + ex.Message);
        }
    }

    private async Task OnCallbackQuery(CallbackQuery callbackQuery)
    {
        if (callbackQuery.Message == null) return;
        var messageId = callbackQuery.Message.MessageId;
        var chatId = callbackQuery.Message.Chat.Id;
        var replyId = callbackQuery.Message.ReplyToMessage?.MessageId;

        Console.WriteLine(
            $"Incoming callback from id:{callbackQuery.From.Id} " +
            $"user:{callbackQuery.From.Username} " +
            $"name:{callbackQuery.From.FirstName} " +
            $"surname:{callbackQuery.From.LastName}");
        if (callbackQuery.Data != null && callbackQuery.Data.Contains('/'))
        {
            //it's command from help
            var msg = callbackQuery.Data.Trim('/');
            try
            {
                var restoredChat = RestoreChatById(chatId);
                await _commands.ParseCommandAsync(msg, restoredChat, replyId);
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
            Console.WriteLine($"Cannot restore chat by {messageId} message");
        }
        else
        {
            var isUpdated = false;
            if (callbackQuery.Data != "Show")
            {
                isUpdated = _commands.UpdatePoll(chatByPoll, messageId, callbackQuery);
            }
            if(isUpdated) await _commands.RenderPollAsync(chatByPoll, messageId);
        }
    }

    private HockeyChat RestoreChatByPollMessageId(int messageId, long chatId)
    {
        var chat = Chats.FindLast(c => c.Polls.Any(poll => poll.MessageId == messageId));
        if (chat != null) return chat;
        chat = RestoreChatById(chatId);
        RestorePollFromDb(messageId, chat);
        return chat;
    }

    private HockeyChat RestoreChatById(long chatId)
    {
        var chat = Chats.FindLast(c => c.Id == chatId);
        if (chat != null) return chat;
        chat = new HockeyChat(chatId);
        Chats.Add(chat);
        return chat;
    }

    private void RestorePollFromDb(int messageId, HockeyChat chat)
    {
        var poll = _db.GetPollByMessageId(messageId);
        if (poll == null) return;

        chat.Polls.Add(poll);

        poll.Votes = _db.GetVotesByMessageId(messageId);
        Console.WriteLine("Poll restored from DB: " + poll.Question);
    }
}