using System.Collections.Generic;
using KLHockeyBot.Data;

namespace KLHockeyBot.Bot
{
    public class Chat
    {
        public long Id { get; set; }

        public bool AddMode { get; set; }
        public bool RemoveMode { get; set; }
        public bool UpdateUseridMode { get; set; }
        public bool VoteMode { get; set; }

        public Queue<string> CommandsQueue { get; set; } = new Queue<string>();
        public List<WaitingVoting> WaitingVotings { get; set; }

        public Chat(long id)
        {
            Id = id;
            WaitingVotings = new List<WaitingVoting>();
        }

        internal void ResetMode()
        {
            AddMode = false;
            RemoveMode = false;
            UpdateUseridMode = false;
            VoteMode = false;
            CommandsQueue.Clear();
        }
    }

    public class WaitingVoting
    {
        public string Question { get; set; }
        public int MessageId { get; set; }
        public int Id { get; set; }
        public List<Vote> V { get; set; }
    }
}
