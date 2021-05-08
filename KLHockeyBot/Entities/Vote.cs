namespace KLHockeyBot.Entities
{
    public class Vote
    {
        public int TelegramUserId { get; set; }
        public int MessageId { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Data { get; set; }

        public Vote(int messageId, int telegramUserId, string username, string name, string surname, string data)
        {
            MessageId = messageId;
            TelegramUserId = telegramUserId;
            Username = username;
            Name = name;
            Surname = surname;
            Data = data;
        }
    }
}