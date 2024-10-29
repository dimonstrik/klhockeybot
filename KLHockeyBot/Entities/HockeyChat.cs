using System.Collections.Generic;

namespace KLHockeyBot.Entities;

public class HockeyChat(long id)
{
    public long Id { get; set; } = id;
    public bool VoteMode { get; set; }

    public Queue<Command> CommandsQueue { get; set; } = new();
    public List<HockeyPoll> Polls { get; set; } = [];
}