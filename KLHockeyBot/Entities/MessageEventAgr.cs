using System;

namespace KLHockeyBot.Entities
{
    public class AdminMessageEventArgs : EventArgs
    {
        public string Command { get; }
        public HockeyChat Chat { get; }
        public Player CurrentPlayer { get; }
        public Poll CurrentPoll { get; }

        internal AdminMessageEventArgs(string command, HockeyChat chat, Player currentPlayer, Poll currentPoll)
        {
            Command = command;
            Chat = chat;
            CurrentPlayer = currentPlayer;
            CurrentPoll = currentPoll;
        }
    }

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
}
