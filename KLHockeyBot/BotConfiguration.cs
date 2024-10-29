namespace KLHockeyBot;

public class BotConfiguration
{
    public string BotToken { get; init; } = default!;
    public string DbFilePath { get; init; } = default!;
    public string DbPlayersInfoFilePath { get; init; } = default!;
    public string DbCreationScriptFilePath { get; init; } = default!;
}