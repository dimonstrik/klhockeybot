using System;

namespace KLHockeyBot.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public int TelegramUserid { get; set; }
        public int Number { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string SecondName { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public string Birthday { get; set; }
        public string PhotoFile { get; set; }

        public Player(int number, string name, string surname, int telegramUserid)
        {

            Number = number;
            Name = name;
            Surname = surname;
            TelegramUserid = telegramUserid;
            PhotoFile = $"{number}_{surname.ToLower()}.jpg";
        }

        public override string ToString()
        {
            return Number + " - " + Name + " " + Surname;
        }
    }
}
