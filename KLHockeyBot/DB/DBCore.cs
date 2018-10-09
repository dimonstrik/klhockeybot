using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using KLHockeyBot.Bot;
using KLHockeyBot.Configs;
using KLHockeyBot.Data;

namespace KLHockeyBot.DB
{
    internal class DbCore
    {
        private string DbFile { get; } = Config.DbFile;

        private const string SqlForCreateon = @"DB/SQLDBCreate.sql";

        SQLiteConnection _conn;

        /// <summary>
        /// При создании класса, сразу подключаем.(если базы нет, он ее создаст)
        /// </summary>
        public DbCore()
        {
            Connect();
        }

        public void Connect()
        {
            try
            {
                _conn = new SQLiteConnection($"Data Source={DbFile}; Version=3;");
                _conn.Open();

                var cmd = _conn.CreateCommand();
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
            _conn.Close();
            _conn.Dispose();
        }

        public void CreateDefaultDb()
        {
            var sql = File.ReadAllText(SqlForCreateon);

            var cmd = _conn.CreateCommand();
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
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM voting WHERE messageid = " + messageId;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var voting = new WaitingVoting() { MessageId = messageId, V = null, Question = reader["question"].ToString() };
                    return voting;
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        public List<Vote> GetVotesByMessageId(int messageId)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM vote WHERE messageid = " + messageId;

            var votes = new List<Vote>();
            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var vote = new Vote( Convert.ToInt32(reader["userid"].ToString()), 
                        reader["username"].ToString(), reader["name"].ToString(), reader["surname"].ToString(), reader["data"].ToString());
                    votes.Add(vote);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return votes;
        }

        public Player GetPlayerByNumber(int number)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM player WHERE number = " + number;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var player = new Player(Convert.ToInt32(reader["number"].ToString()),
                        reader["name"].ToString(),
                        reader["lastname"].ToString(),
                        Convert.ToInt32(reader["userid"].ToString()))
                    {
                        Id = Convert.ToInt32(reader["id"].ToString()),
                        Birthday = reader["birthday"].ToString(),
                        Status = reader["status"].ToString(),
                        Position = reader["position"].ToString(),
                        SecondName = reader["secondname"].ToString(),
                    };
                    return player;
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return null;
        }

        public Player GetPlayerById(int id)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM player WHERE id = " + id;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var player = new Player(Convert.ToInt32(reader["number"].ToString()),
                        reader["name"].ToString(),
                        reader["lastname"].ToString(),
                        Convert.ToInt32(reader["userid"].ToString()))
                    {
                        Id = Convert.ToInt32(reader["id"].ToString()),
                        Birthday = reader["birthday"].ToString(),
                        Status = reader["status"].ToString(),
                        Position = reader["position"].ToString(),
                        SecondName = reader["secondname"].ToString(),
                    };
                    return player;
                }

            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            return null;
        }

        public List<Player> GetAllPlayerWitoutStatistic()
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM player";

            var players = new List<Player>();
            try
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var number = Convert.ToInt32(reader["number"].ToString());
                    var name = reader["name"].ToString();
                    var lastname = reader["lastname"].ToString();
                    var userid = Convert.ToInt32(reader["userid"].ToString());

                    var player = new Player(number, name, lastname, userid)
                    {
                        Id = Convert.ToInt32(reader["id"].ToString()),
                        Birthday = reader["birthday"].ToString(),
                        Status = reader["status"].ToString(),
                        Position = reader["position"].ToString(),
                        SecondName = reader["secondname"].ToString(),
                    };
                    players.Add(player);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
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

        public List<Data.Event> GetEventsByType(string type)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM event WHERE type = '{type}'";

            var events = new List<Data.Event>();
            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var even = new Data.Event
                    {
                        Id = int.Parse(reader["id"].ToString()),
                        Type = reader["type"].ToString(),
                        Date = reader["date"].ToString(),
                        Time = reader["time"].ToString(),
                        Place = reader["place"].ToString(),
                        Address = reader["address"].ToString(),
                        Details = reader["details"].ToString(),
                        Result = reader["result"].ToString()
                    };

                    events.Add(even);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return events;
        }

        public List<Player> GetPlayersByNameOrSurname(string nameOrSurname)
        {

            var cmd = _conn.CreateCommand();
            cmd.CommandText =$"SELECT * FROM player WHERE lastname_lower = '{nameOrSurname.ToLower()}' OR name = '{nameOrSurname}'";

            var players = new List<Player>();
            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var player = new Player(
                        Convert.ToInt32(reader["number"].ToString()),
                        reader["name"].ToString(),
                        reader["lastname"].ToString(),
                        Convert.ToInt32(reader["userid"].ToString()))
                    {
                        Id = Convert.ToInt32(reader["id"].ToString()),
                        Birthday = reader["birthday"].ToString(),
                        Status = reader["status"].ToString(),
                        Position = reader["position"].ToString(),
                        SecondName = reader["secondname"].ToString(),
                    };
                    players.Add(player);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return players;
        }

        public void AddVoting(WaitingVoting voting)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = $"INSERT INTO voting (messageid, question) VALUES({voting.MessageId}, '{voting.Question}')";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void AddVote(int messageId, int userid, string username, string name, string surname, string data)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
                $"INSERT INTO vote (messageid, userid, username, name, surname, data) " +
                   $"VALUES({messageId}, {userid}, '{username}', '{name}', '{surname}', '{data}')";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void UpdateVoteData(int messageId, int userid, string data)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
                   $"UPDATE vote SET data='{data}' WHERE messageid={messageId} AND userid='{userid}'";

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
            var cmd = _conn.CreateCommand();
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
            var cmd = _conn.CreateCommand();
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
            if(File.Exists(Config.DbFile))File.Delete(Config.DbFile);
            var db = new DbCore();

            Console.WriteLine("CreateDB");
            db.CreateDefaultDb();

            Console.WriteLine("FillPlayersFromFile");
            db.LoadPlayersFromFile();

            Console.WriteLine("FillEventsFromFile");
            db.LoadEventsFromFile();
                        
            db.Disconnect();
            Console.WriteLine("Finish Initialization");
        }

        private void ClearEvents()
        {
            var cmd = _conn.CreateCommand();
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
            var cmd = _conn.CreateCommand();
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
            var players = File.ReadAllLines(Config.DbPlayersInfoPath);

            foreach (var player in players)
            {
                var playerinfo = player.Split(';');
                var cmd = _conn.CreateCommand();
                
                try
                {
                    //1;Зверев;Алексей;Александрович;23.07.1986;вр;Вратарь;12345
                    cmd.CommandText =
                        "INSERT INTO player (number, lastname, lastname_lower," +
                        $"name, secondname, birthday, position, status, " +
                        $"userid) " +
                        $"VALUES({playerinfo[0].Trim()}, '{playerinfo[1].Trim()}', '{playerinfo[1].Trim().ToLower()}', " +
                        $"'{playerinfo[2].Trim()}', '{playerinfo[3].Trim()}', '{playerinfo[4].Trim()}', '{playerinfo[5].Trim()}', '{playerinfo[6].Trim()}'," +
                        $"'{playerinfo[7].Trim()}')";

                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
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
            public string result;
        }

        public void LoadEventsFromFile()
        {

            var events = File.ReadAllLines(Config.DbEventsInfoPath);

            foreach (var even in events)
            {
                var fields = even.Split(';');
                //type=Игра;date=30 октября;time=11:00;place=Янтарь;address=г.Москва, ул.Маршала Катукова, д.26;details=Сезон 2016-2017 дивизион КБЧ-Восток%Янтарь-2 Wild Woodpeckers;0:0
                var ev = new Event
                {
                    type = "",
                    date = "",
                    time = "",
                    place = "",
                    address = "",
                    details = "",
                    result = ""
                };
                foreach (var field in fields)
                {
                    var keyvalue = field.Split('=');
                    if (keyvalue[0] == "type") ev.type = keyvalue[1];
                    if (keyvalue[0] == "date") ev.date = keyvalue[1];
                    if (keyvalue[0] == "time") ev.time = keyvalue[1];
                    if (keyvalue[0] == "place") ev.place = keyvalue[1];
                    if (keyvalue[0] == "address") ev.address = keyvalue[1];
                    if (keyvalue[0] == "details") ev.details = keyvalue[1];
                    if (keyvalue[0] == "result") ev.result = keyvalue[1];
                }

                ev.details = ev.details.Replace('%', '\n');

                var cmd = _conn.CreateCommand();
                cmd.CommandText = $"INSERT INTO event (type, date, time, place, address, details, result) VALUES('{ev.type}', '{ev.date}', '{ev.time}', '{ev.place}', '{ev.address}', '{ev.details}', '{ev.result}')";                

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

        public Player GetPlayerByUserid(int userId)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM player WHERE userid = " + userId;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var player = new Player(Convert.ToInt32(reader["number"].ToString()),
                        reader["name"].ToString(),
                        reader["lastname"].ToString(),
                        Convert.ToInt32(reader["userid"].ToString()))
                    {
                        Id = Convert.ToInt32(reader["id"].ToString()),
                        Birthday = reader["birthday"].ToString(),
                        Status = reader["status"].ToString(),
                        Position = reader["position"].ToString(),
                        SecondName = reader["secondname"].ToString(),
                    };
                    return player;
                }

            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        public void UpdatePlayerUserid(int id, int userid)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
                $"UPDATE player SET userid={userid} WHERE id={id}";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public List<Vote> GetAllUniqueVotesByUserid()
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM vote GROUP BY userid";

            var votes = new List<Vote>();
            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var vote = new Vote(Convert.ToInt32(reader["userid"].ToString()), 
                        reader["username"].ToString(), reader["name"].ToString(), reader["surname"].ToString(), reader["data"].ToString());votes.Add(vote);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return votes;
        }
    }
}
