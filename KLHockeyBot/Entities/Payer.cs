namespace KLHockeyBot.Entities
{
    public class Payer
    {
        public long TelegramUserId { get; set; }
        public int MessageId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public long Amount { get; set; }

        public Payer(int messageId, long telegramUserId, string username, string name, string surname, long amount)
        {
            MessageId = messageId;
            TelegramUserId = telegramUserId;
            Username = username;
            Name = name;
            Surname = surname;
            Amount = amount;
        }
    }
}