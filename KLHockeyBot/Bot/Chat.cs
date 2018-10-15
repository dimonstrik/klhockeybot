using System.Collections.Generic;

namespace KLHockeyBot.Bot
{
    public class Chat
    {
        public long Id { get; set; }

        public bool AddMode { get; set; }
        public bool EditMode { get; internal set; }
        public bool RemoveMode { get; set; }
        public bool UpdateUseridMode { get; set; }
        public bool VoteMode { get; set; }

        public Queue<string> CommandsQueue { get; set; } = new Queue<string>();
        public List<Poll> Polls { get; set; }

        public Chat(long id)
        {
            Id = id;
            Polls = new List<Poll>();
        }

        internal void ResetMode()
        {
            AddMode = false;
            EditMode = false;
            RemoveMode = false;
            UpdateUseridMode = false;
            VoteMode = false;
            CommandsQueue.Clear();
        }
    }

}
