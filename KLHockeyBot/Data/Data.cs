using System;
using System.Collections.Generic;
using System.Linq;

namespace KLHockeyBot
{
    public class Vote
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Data { get; set; }

        public Vote(string name, string surname, string data)
        {
            Name = name;
            Surname = surname;
            Data = data;
        }
    }
    public class Player
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Nickname { get; set; }
        public string Surname { get; set; }
        public string PhotoFile { get; set; }
        public string Position { get; set; }
        public string SecondName { get; set; }
        public string Status { get; set; }
        public string Birthday { get; set; }
        
        public Player(int number, string name, string surname, string nickname)
        {

            Number = number;
            Name = name;
            Surname = surname;
            Nickname = nickname;
            PhotoFile = $"{number}_{surname.ToLower()}.jpg";
        }

        public override string ToString()
        {
            return Number + " - " + Name + " " + Surname;
        }

        public string Description => $"👥*{Number} {Surname}*\n{Name} {SecondName}\n{Position} {Status}\n{Birthday}";
    }

    public class Event
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Place { get; set; }
        public string Address { get; set; }
        public string Details { get; set; }
        public string Result { get; set; }

        public Event()
        {

        }
    }
   
}
