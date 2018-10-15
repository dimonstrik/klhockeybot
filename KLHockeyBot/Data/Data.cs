using System;
using KLHockeyBot.Bot;

namespace KLHockeyBot.Data
{
    public class Vote
    {
        public int Userid { get; set; }
        public int Msgid { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Data { get; set; }

        public Vote(int msgid, int userid, string username, string name, string surname, string data)
        {
            Msgid = msgid;
            Userid = userid;
            Username = username;
            Name = name;
            Surname = surname;
            Data = data;
        }
    }

    public class Player
    {
        public int Id { get; set; }
        public int Userid { get; set; }
        public int Number { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string SecondName { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public string Birthday { get; set; }
        public string PhotoFile { get; set; }

        public Player(int number, string name, string surname, int userid)
        {

            Number = number;
            Name = name;
            Surname = surname;
            Userid = userid;
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
    }

    public class AdminMessageEventArgs : EventArgs
    {
        public string Command { get; }
        public Chat Chat { get; }
        public Player CurrentPlayer { get; }
        public Poll CurrentPoll { get; }

        internal AdminMessageEventArgs(string command, Chat chat, Player currentPlayer, Poll currentPoll)
        {
            Command = command;
            Chat = chat;
            CurrentPlayer = currentPlayer;
            CurrentPoll = currentPoll;
        }
    }
}
