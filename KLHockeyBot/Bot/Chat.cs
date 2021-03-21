using System.Collections.Generic;
using KLHockeyBot.Data;

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
        public  bool EventAddMode { get; set; }

        public Queue<Command> CommandsQueue { get; set; } = new Queue<Command>();
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
            EventAddMode = false;
            CommandsQueue.Clear();
        }
    }

}
