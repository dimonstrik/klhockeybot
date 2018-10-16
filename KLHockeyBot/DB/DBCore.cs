using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using KLHockeyBot.Bot;
using KLHockeyBot.Configs;
using KLHockeyBot.Data;

namespace KLHockeyBot.DB
{
    public class DbCore
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

        public Poll GetPollByMessageId(int messageId)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM voting WHERE messageid = " + messageId;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var voting = new Poll() { Id = Convert.ToInt32(reader["id"].ToString()), 
                        MessageId = messageId, 
                        Votes = null, 
                        Question = reader["question"].ToString() };
                    return voting;
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        public Poll GetPollById(int id)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM voting WHERE id = " + id;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var poll = new Poll() { Id = id, 
                        MessageId = Convert.ToInt32(reader["messageid"].ToString()), 
                        Votes = null, 
                        Question = reader["question"].ToString() };
                    return poll;
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
                    var vote = new Vote(Convert.ToInt32(reader["messageid"].ToString()), Convert.ToInt32(reader["userid"].ToString()), 
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

        public List<Player> GetAllPlayers()
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

        public List<Poll> GetAllPolls()
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM voting";

            var polls = new List<Poll>();
            try
            {
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var poll = new Poll()
                    {
                        Id = Convert.ToInt32(reader["id"].ToString()),
                        MessageId = Convert.ToInt32(reader["messageid"].ToString()),
                        Votes = null,
                        Question = reader["question"].ToString()
                    };
                    polls.Add(poll);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return polls;
        }

        public List<Event> GetEventsByType(string type)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM event WHERE type = '{type}'";

            var events = new List<Event>();
            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var even = new Event
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

        public void AddPoll(Poll voting)
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

        public void DeletePoll(Poll poll)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
                $"DELETE FROM voting WHERE " +
                $"messageid={poll.MessageId} and " +
                $"id={poll.Id}";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void AddVote(Vote vote)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
                $"INSERT INTO vote (messageid, userid, username, name, surname, data) " +
                   $"VALUES({vote.MessageId}, {vote.TelegramUserId}, '{vote.Username}', '{vote.Name}', '{vote.Surname}', '{vote.Data}')";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void DeleteVote(Vote vote)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
                $"DELETE FROM vote WHERE " +
                $"messageid={vote.MessageId} and " +
                $"userid={vote.TelegramUserId} and " +
                $"username='{vote.Username}' and " +
                $"name='{vote.Name}' and " +
                $"surname='{vote.Surname}' and " +
                $"data='{vote.Data}'";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        internal void UpdatePlayer(Player currentPlayer, Player player)
        {
            var cmd = _conn.CreateCommand();
            //1;Зверев;Алексей;Александрович;23.07.1986;вр;Вратарь;12345
            cmd.CommandText =
                "UPDATE player SET " +
                   $"number={player.Number}, lastname='{player.Surname}', lastname_lower='{player.Surname.ToLower()}', " +
                   $"name='{player.Name}', secondname='{player.SecondName}', birthday='{player.Birthday}', position='{player.Position}', status='{player.Status}'," +
                   $"userid='{player.TelegramUserid}' " +
                   $"WHERE id={currentPlayer.Id}";

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
            //1;Зверев;Алексей;Александрович;23.07.1986;вр;Вратарь;12345
            cmd.CommandText =
                "INSERT INTO player (number, lastname, lastname_lower," +
                $"name, secondname, birthday, position, status, " +
                $"userid) " +
                   $"VALUES({player.Number}, '{player.Surname}', '{player.Surname.ToLower()}', " +
                   $"'{player.Name}', '{player.SecondName}', '{player.Birthday}', '{player.Position}', '{player.Status}'," +
                   $"'{player.TelegramUserid}')";

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void RemovePlayerById(int id)
        {
            var cmd = _conn.CreateCommand();
            var player = GetPlayerById(id);
            if (player == null) return;

            cmd.CommandText = $"DELETE from player where id={id}";

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

        public void LoadEventsFromFile()
        {

            var events = File.ReadAllLines(Config.DbEventsInfoPath);

            foreach (var even in events)
            {
                var fields = even.Split(';');
                //type=Игра;date=30 октября;time=11:00;place=Янтарь;address=г.Москва, ул.Маршала Катукова, д.26;details=Сезон 2016-2017 дивизион КБЧ-Восток%Янтарь-2 Wild Woodpeckers;0:0
                var ev = new TxtEvent
                {
                    Type = "",
                    Date = "",
                    Time = "",
                    Place = "",
                    Address = "",
                    Details = "",
                    Result = ""
                };
                foreach (var field in fields)
                {
                    var keyvalue = field.Split('=');
                    if (keyvalue[0] == "type") ev.Type = keyvalue[1];
                    if (keyvalue[0] == "date") ev.Date = keyvalue[1];
                    if (keyvalue[0] == "time") ev.Time = keyvalue[1];
                    if (keyvalue[0] == "place") ev.Place = keyvalue[1];
                    if (keyvalue[0] == "address") ev.Address = keyvalue[1];
                    if (keyvalue[0] == "details") ev.Details = keyvalue[1];
                    if (keyvalue[0] == "result") ev.Result = keyvalue[1];
                }

                ev.Details = ev.Details.Replace('%', '\n');

                var cmd = _conn.CreateCommand();
                cmd.CommandText = $"INSERT INTO event (type, date, time, place, address, details, result) VALUES('{ev.Type}', '{ev.Date}', '{ev.Time}', '{ev.Place}', '{ev.Address}', '{ev.Details}', '{ev.Result}')";                

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

        public void UpdatePlayerUserid(int id, int telegramUserId)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText =
                $"UPDATE player SET userid={telegramUserId} WHERE id={id}";

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
                    var vote = new Vote(Convert.ToInt32(reader["messageid"].ToString()), Convert.ToInt32(reader["userid"].ToString()), 
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
