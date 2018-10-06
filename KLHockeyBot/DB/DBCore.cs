using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.SQLite;
using KLHockeyBot.Configs;
using System.Globalization;

namespace KLHockeyBot
{
    class DBCore
    {
        string DBFile => Config.DBFile;
        readonly string SQLForCreateon = @"DB/SQLDBCreate.sql";

        SQLiteConnection conn;

        /// <summary>
        /// При создании класса, сразу подключаем.(если базы нет, он ее создаст)
        /// </summary>
        public DBCore()
        {
            Connect();
        }

        public void Connect()
        {
            try
            {
                conn = new SQLiteConnection($"Data Source={DBFile}; Version=3;");
                conn.Open();

                SQLiteCommand cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA foreign_keys = 1";
                cmd.ExecuteNonQuery();

            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Disconnect()
        {
            conn.Close();
            conn.Dispose();
        }

        public void CreateDefaultDB()
        {
            string sql = File.ReadAllText(SQLForCreateon);

            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public WaitingVoting GetVotingById(int messageId)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM voting WHERE messageid = " + messageId;

            SQLiteDataReader reader = null;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            while (reader.Read() && reader.HasRows)
            {
                var voting = new WaitingVoting() { MessageId = messageId, V = null, Question = reader["question"].ToString() };
                return voting;
            }
            return null;
        }

        public List<Vote> GetVotesByMessageId(int messageId)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM vote WHERE messageid = " + messageId;

            SQLiteDataReader reader = null;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            var votes = new List<Vote>();
            while (reader.Read() && reader.HasRows)
            {
                var vote = new Vote(reader["name"].ToString(), reader["surname"].ToString(), reader["data"].ToString());
                votes.Add(vote);
            }
            return votes;
        }

        public Player GetPlayerByNumber(int number)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM player WHERE number = " + number;

            SQLiteDataReader reader = null;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            while (reader.Read())
            {
                var player = new Player(Convert.ToInt32(reader["number"].ToString()), 
                    reader["name"].ToString(),
                    reader["lastname"].ToString());
                player.Id = Convert.ToInt32(reader["id"].ToString());
                return player;
            }
            return null;
        }

        public Player GetPlayerById(int id)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM player WHERE id = " + id;

            SQLiteDataReader reader = null;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            while (reader.Read())
            {
                var player = new Player(Convert.ToInt32(reader["number"].ToString()),
                    reader["name"].ToString(),
                    reader["lastname"].ToString());
                player.Id = Convert.ToInt32(reader["id"].ToString());
                return player;
            }
            return null;
        }

        public List<Player> GetAllPlayerWitoutStatistic()
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM player";

            SQLiteDataReader reader = null;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            var players = new List<Player>();
            while (reader.Read())
            {
                int number = Convert.ToInt32(reader["number"].ToString());
                string name = reader["name"].ToString();
                string lastname = reader["lastname"].ToString();

                var player = new Player(number, name, lastname);
                player.Id = Convert.ToInt32(reader["id"].ToString());
                player.Position = reader["position"].ToString();
                players.Add(player);
            }
            return players;
        }

        public Player GetPlayerStatistic(Player player)
        {                       
            return player;
        }

        public Player GetPlayerStatisticByNumber(int number)
        {
            var player = GetPlayerByNumber(number);
            return GetPlayerStatistic(player);
        }       

        public List<KLHockeyBot.Event> GetEventsByType(string type)
        {
            var events = new List<KLHockeyBot.Event>();

            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM event WHERE type = '{type}'";

            SQLiteDataReader reader = null;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            while (reader.Read())
            {
                var even = new KLHockeyBot.Event();
                even.Id = int.Parse(reader["id"].ToString());
                even.Type = reader["type"].ToString();
                even.Date = reader["date"].ToString();
                even.Time = reader["time"].ToString();
                even.Place = reader["place"].ToString();
                even.Address = reader["address"].ToString();
                even.Details = reader["details"].ToString();
                even.Members = reader["members"].ToString();

                events.Add(even);
            }

            return events;
        }

        public List<Player> GetPlayersByNameOrSurname(string nameOrSurname)
        {
            var players = new List<Player>();

            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText =$"SELECT * FROM player WHERE lastname_lower = '{nameOrSurname.ToLower()}' OR name = '{nameOrSurname}'";

            SQLiteDataReader reader = null;
            try
            {
                reader = cmd.ExecuteReader();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            while (reader.Read())
            {
                var player = new Player(
                    Convert.ToInt32(reader["number"].ToString()), 
                    reader["name"].ToString(),
                    reader["lastname"].ToString());
                player.Id = Convert.ToInt32(reader["id"].ToString());
                players.Add(player);
            }

            return players;
        }

        public void AddVoting(WaitingVoting voting)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("INSERT INTO voting (messageid, question) VALUES({0}, '{1}')",
                voting.MessageId, voting.Question);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void AddVote(int messageId, string name, string surname, string data)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("INSERT INTO vote (messageid, name, surname, data) VALUES({0}, '{1}', '{2}', '{3}')",
                messageId, name, surname, data);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void UpdateVoteData(int messageId, string name, string surname, string data)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("UPDATE vote SET data='{3}' WHERE messageid={0} AND name='{1}' AND surname='{2}'",
                messageId, name, surname, data);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void AddPlayer(Player player)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = string.Format("INSERT INTO player (number, name, lastname) VALUES({0}, '{1}', '{2}')",
                player.Number, player.Name, player.Surname);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void RemovePlayerByNumber(int number)
        {
            SQLiteCommand cmd = conn.CreateCommand();
            var player = GetPlayerByNumber(number);
            if (player == null) return;

            cmd.CommandText = string.Format("DELETE from player where number={0}", number);

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void Initialization()
        {
            Console.WriteLine("Start Initialization");
            File.Delete(Config.DBFile);
            DBCore db = new DBCore();

            Console.WriteLine("CreateDB");
            db.CreateDefaultDB();

            Console.WriteLine("FillPlayersFromFile");
            db.LoadPlayersFromFile();

            Console.WriteLine("FillEventsFromFile");
            db.LoadEventsFromFile();
                        
            db.Disconnect();
            Console.WriteLine("Finish Initialization");
        }

        public static void InitializationOnlyEvents()
        {
            Console.WriteLine("Start Initialization for Events");
            DBCore db = new DBCore();

            Console.WriteLine("Clear events");
            db.ClearEvents();

            Console.WriteLine("FillEventsFromFile");
            db.LoadEventsFromFile();

            db.Disconnect();
            Console.WriteLine("Finish Initialization");
        }

        public static void InitializationOnlyPlayers()
        {
            Console.WriteLine("Start Initialization for Events");
            DBCore db = new DBCore();

            Console.WriteLine("Clear players");
            db.ClearPlayers();

            Console.WriteLine("FillEventsFromFile");
            db.LoadPlayersFromFile();

            db.Disconnect();
            Console.WriteLine("Finish Initialization");
        }

        private void ClearEvents()
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM event";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void ClearPlayers()
        {
            SQLiteCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM player";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #region Import

        public void LoadPlayersFromFile()
        {
            var players = File.ReadAllLines(Config.DBPlayersInfoPath);

            foreach (var player in players)
            {
                var playerinfo = player.Split(';');

                SQLiteCommand cmd = conn.CreateCommand();
                cmd.CommandText = string.Format("INSERT INTO player (number, name, lastname, lastname_lower) VALUES({0}, '{1}', '{2}', '{3}')",
                    playerinfo[0].Trim(), playerinfo[2].Trim(), playerinfo[1].Trim(), playerinfo[1].Trim().ToLower());

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        struct Event
        {
            public string type;
            public string date;
            public string time;
            public string place;
            public string address;
            public string details;
            public string be;
            public string maybe;
            public string notbe;
        }

        public void LoadEventsFromFile()
        {

            var events = File.ReadAllLines(Config.DBEventsInfoPath);

            foreach (var even in events)
            {
                var fields = even.Split(';');
                //type=Игра;date=30 октября;time=11:00;place=Янтарь;address=г.Москва, ул.Маршала Катукова, д.26;details=Сезон 2016-2017 дивизион КБЧ-Восток%Янтарь-2 Wild Woodpeckers%Будут:1 Возможно:1 Не будут:1;be=Игорь Смирнов;maybe=Латохин Дмитрий;notbe=Скалин Петр
                var ev = new Event();
                ev.type = "";
                ev.date = "";
                ev.time = "";
                ev.place = "";
                ev.address = "";
                ev.details = "";
                ev.be = "*Будут:*\n";
                ev.maybe = "\n*Возможно:*\n";
                ev.notbe = "\n*Не будут:*\n";
                foreach (var field in fields)
                {
                    var keyvalue = field.Split('=');
                    if (keyvalue[0] == "type") ev.type = keyvalue[1];
                    if (keyvalue[0] == "date") ev.date = keyvalue[1];
                    if (keyvalue[0] == "time") ev.time = keyvalue[1];
                    if (keyvalue[0] == "place") ev.place = keyvalue[1];
                    if (keyvalue[0] == "address") ev.address = keyvalue[1];
                    if (keyvalue[0] == "details") ev.details = keyvalue[1];
                    if (keyvalue[0] == "be") ev.be += keyvalue[1] + "\n";
                    if (keyvalue[0] == "maybe") ev.maybe += keyvalue[1] + "\n";
                    if (keyvalue[0] == "notbe") ev.notbe += keyvalue[1] + "\n";
                }

                ev.details = ev.details.Replace('%', '\n');

                if (ev.be == "*Будут:*\n") ev.be = "";
                if (ev.maybe == "\n*Возможно:*\n") ev.maybe = "";
                if (ev.notbe == "\n*Не будут:*\n") ev.notbe = "";

                SQLiteCommand cmd = conn.CreateCommand();
                cmd.CommandText = $"INSERT INTO event (type, date, time, place, address, details, members) VALUES('{ev.type}', '{ev.date}', '{ev.time}', '{ev.place}', '{ev.address}', '{ev.details}', '{ev.be + ev.maybe + ev.notbe}')";                

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            
        }
        #endregion

    }
}
