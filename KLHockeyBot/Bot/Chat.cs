using System;
using System.Collections.Generic;

namespace KLHockeyBot
{
    public class Chat
    {
        public long Id { get; set; }

        public bool AddMode { get; set; } = false;
        public bool RemoveMode { get; set; } = false;
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
            CommandsQueue.Clear();
        }
    }

    public class WaitingVoting
    {
        public string Question { get; set; }
        public int MessageId { get; set; }
        public List<Vote> V { get; set; }
    }
}
