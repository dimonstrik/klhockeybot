using System;

namespace KLHockeyBot.Entities;

public class PollMessageEventArgs : EventArgs
{
    public string Cmd { get; }
    public string Arg { get; }
    public HockeyChat Chat { get; }
    public int ReplyId { get; }

    internal PollMessageEventArgs(string cmd, string arg, HockeyChat chat, int replyId)
    {
        Cmd = cmd;
        Arg = arg;
        Chat = chat;
        ReplyId = replyId;
    }
}