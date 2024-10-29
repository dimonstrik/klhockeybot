namespace KLHockeyBot.Entities;

public class Payer(int messageId, long telegramUserId, string username, string name, string surname, long amount)
{
    public long TelegramUserId { get; set; } = telegramUserId;
    public int MessageId { get; set; } = messageId;
    public string Username { get; set; } = username;
    public string Name { get; set; } = name;
    public string Surname { get; set; } = surname;
    public long Amount { get; set; } = amount;
}