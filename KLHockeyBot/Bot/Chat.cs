using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace KLHockeyBot
{
    public class Chat
    {
        public long Id { get; set; }

        public bool WhoMode { get; set; } = false;
        public bool PersonalStatMode { get; set; } = false;
        public bool AddMode { get; set; } = false;
        public bool RemoveMode { get; set; } = false;
        public bool VoteMode { get; set; }

        public Queue<string> CommandsQueue { get; set; } = new Queue<string>();
        public List<WaitingStatistic> WaitingStatistics { get; set; }
        public List<WaitingEvent> WaitingEvents { get; set; }
        public List<WaitingVoting> WaitingVotings { get; set; }

        public Chat(long id)
        {
            Id = id;
            WaitingStatistics = new List<WaitingStatistic>();
            WaitingEvents = new List<WaitingEvent>();
            WaitingVotings = new List<WaitingVoting>();
        }

        internal void ResetMode()
        {
            WhoMode = false;
            PersonalStatMode = false;
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

        public void SaveDb()
        {
            throw new NotImplementedException();
        }
    }

    public class WaitingStatistic
    {
        public Message Msg { get; set; }
        public Player Plr { get; set; }
    }

    public class WaitingEvent
    {
        public Message Msg { get; set; }
        public Event Even { get; set; }
    }
}
