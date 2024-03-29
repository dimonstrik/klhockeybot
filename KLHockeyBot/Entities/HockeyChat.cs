﻿using System.Collections.Generic;

namespace KLHockeyBot.Entities
{
    public class HockeyChat
    {
        public long Id { get; set; }

        public bool AddMode { get; set; }
        public bool EditMode { get; internal set; }
        public bool RemoveMode { get; set; }
        public bool UpdateUseridMode { get; set; }
        public bool VoteMode { get; set; }
        public  bool EventAddMode { get; set; }

        public Queue<Command> CommandsQueue { get; set; } = new Queue<Command>();
        public List<HockeyPoll> Polls { get; set; }
        public List<HockeyPayment> Payments { get; set; }

        public HockeyChat(long id)
        {
            Id = id;
            Polls = new List<HockeyPoll>();
            Payments = new List<HockeyPayment>();
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
