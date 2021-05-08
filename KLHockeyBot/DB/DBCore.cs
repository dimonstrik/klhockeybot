using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using KLHockeyBot.Configs;
using KLHockeyBot.Entities;

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
        public HockeyPoll GetPollByMessageId(int messageId)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM voting WHERE messageid = " + messageId;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var voting = new HockeyPoll() { Id = Convert.ToInt32(reader["id"].ToString()), 
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
        public HockeyPayment GetPaymentByMessageId(int messageId)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM payment WHERE messageid = " + messageId;

            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var payment = new HockeyPayment() { Id = Convert.ToInt32(reader["id"].ToString()), 
                        MessageId = messageId, 
                        Payers = null, 
                        Name = reader["name"].ToString() };
                    return payment;
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
                    var vote = new Vote(Convert.ToInt32(reader["messageid"].ToString()), Convert.ToInt64(reader["userid"].ToString()), 
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
        public List<Payer> GetPayersByMessageId(int messageId)
        {
            var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM payer WHERE messageid = " + messageId;

            var votes = new List<Payer>();
            try
            {
                var reader = cmd.ExecuteReader();

                while (reader.Read() && reader.HasRows)
                {
                    var vote = new Payer(Convert.ToInt32(reader["messageid"].ToString()), Convert.ToInt64(reader["userid"].ToString()), 
                        reader["username"].ToString(), reader["name"].ToString(), reader["surname"].ToString(), Convert.ToInt64(reader["amount"]));
                    votes.Add(vote);
                }
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine(ex.Message);
            }
            return votes;
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
                    var userid = Convert.ToInt64(reader["userid"].ToString());

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
        public void AddPoll(HockeyPoll voting)
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
        public void UpdateVoteData(int messageId, long userid, string data)
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
        public static void Initialization()
        {
            Console.WriteLine("Start Initialization");
            if(File.Exists(Config.DbFile))File.Delete(Config.DbFile);
            var db = new DbCore();

            Console.WriteLine("CreateDB");
            db.CreateDefaultDb();

            Console.WriteLine("FillPlayersFromFile");
            db.LoadPlayersFromFile();
                        
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
        #endregion
        public Player GetPlayerByUserid(long userId)
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
                        Convert.ToInt64(reader["userid"].ToString()))
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
    }
}
