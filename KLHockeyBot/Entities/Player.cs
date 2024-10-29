namespace KLHockeyBot.Entities;

public class Player(int number, string name, string surname, long telegramUserid)
{
    public int Id { get; set; }
    public long TelegramUserid { get; set; } = telegramUserid;
    public int Number { get; set; } = number;
    public string Surname { get; set; } = surname;
    public string Name { get; set; } = name;
    public string SecondName { get; set; }
    public string Position { get; set; }
    public string Status { get; set; }
    public string Birthday { get; set; }

    public override string ToString()
    {
        return Number + " - " + Name + " " + Surname;
    }
}