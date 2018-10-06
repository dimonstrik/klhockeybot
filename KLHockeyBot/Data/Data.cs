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
        public string Surname { get; set; }
        public string PhotoFile { get; set; }
        public string Position { get; set; }
               
        public Player(int number, string name, string surname)
        {

            Number = number;
            Name = name;
            Surname = surname;
            PhotoFile = string.Format("{0}_{1}.jpg", number, surname.ToLower());
        }

        public override string ToString()
        {
            return Number + " - " + Name + " " + Surname;
        }
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
        public string Members { get; set; }

        public Event()
        {

        }
    }
   
}
