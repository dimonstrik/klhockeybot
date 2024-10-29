using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using KLHockeyBot.Entities;
using Microsoft.Extensions.Options;

namespace KLHockeyBot.Database;

public class BotDatabase
{
    private string DbFilePath { get; }
    private string DbPlayersInfoFilePath { get; }
    private string DbCreationScriptFilePath { get; }
    private SQLiteConnection _conn;

    public BotDatabase(IOptions<BotConfiguration> config)
    {
        DbFilePath = config.Value.DbFilePath;
        DbPlayersInfoFilePath = config.Value.DbPlayersInfoFilePath;
        DbCreationScriptFilePath = config.Value.DbCreationScriptFilePath;
        Connect();
    }
    public void Connect()
    {
        try
        {
            _conn = new SQLiteConnection($"Data Source={DbFilePath}; Version=3;");
            _conn.Open();

            var cmd = _conn.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = 1";
            cmd.ExecuteNonQuery();

        }
        catch (SQLiteException ex)
        {
            Console.WriteLine( $"Connect exception: {ex.Message}");
        }
    }
    public void Disconnect()
    {
        _conn.Close();
        _conn.Dispose();
    }
    public void CreateDefaultDb()
    {
        var sql = File.ReadAllText(DbCreationScriptFilePath);

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
                var voting = new HockeyPoll()
                {
                    Id = Convert.ToInt32(reader["id"].ToString()),
                    MessageId = messageId,
                    Votes = null,
                    Question = reader["question"].ToString()
                };
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
                var payment = new HockeyPayment()
                {
                    Id = Convert.ToInt32(reader["id"].ToString()),
                    MessageId = messageId,
                    Payers = null,
                    Name = reader["name"].ToString()
                };
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
    public void Initialize()
    {
        Console.WriteLine("Create bot database");
        CreateDefaultDb();

        Console.WriteLine("Fill bot database with players from file");
        LoadPlayersFromFile();
    }

    #region Import
    public void LoadPlayersFromFile()
    {
        var players = File.ReadAllLines(DbPlayersInfoFilePath);

        foreach (var player in players)
        {
            var playerInfo = player.Split(';');
            var cmd = _conn.CreateCommand();

            try
            {
                //1;Зверев;Алексей;Александрович;23.07.1986;вр;Вратарь;12345
                cmd.CommandText =
                    "INSERT INTO player (number, lastname, lastname_lower," +
                    "name, secondname, birthday, position, status, " +
                    "userid) " +
                    $"VALUES({playerInfo[0].Trim()}, '{playerInfo[1].Trim()}', '{playerInfo[1].Trim().ToLower()}', " +
                    $"'{playerInfo[2].Trim()}', '{playerInfo[3].Trim()}', '{playerInfo[4].Trim()}', '{playerInfo[5].Trim()}', '{playerInfo[6].Trim()}'," +
                    $"'{playerInfo[7].Trim()}')";

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

    public string GetLastEventDetails(int n)
    {
        var result = "Wrong limit request";
        if (n <= 0)
            return result;

        var tmp = " " + GetInnerPlayersEventsStat(n) + "\n\n";
        tmp += GetOuterPlayersEventsStat(n) + "\n\n";
        tmp += GetEvents(n);
        result = tmp;
        return result;
    }

    private string GetEvents(int n)
    {
        var result = "";
        var cmd = _conn.CreateCommand();
        //TODO move path to config
        cmd.CommandText = File.ReadAllText("Database/get_last_event_names.sql").Replace("@replace_top_n", n.ToString());

        try
        {
            var reader = cmd.ExecuteReader();
            result += $"*Список последних {n} событий* \n";
            result += "```\n";

            while (reader.Read() && reader.HasRows)
            {
                var ev = reader["ev"].ToString();
                result += $"{ev}\n";
            }

            result += "```";
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine(ex.Message);
        }
        return result;
    }

    private string GetOuterPlayersEventsStat(int n)
    {
        var result = "";
        var cmd = _conn.CreateCommand();
        //TODO move path to config
        cmd.CommandText = File.ReadAllText("Database/get_players_event_count_outer.sql").Replace("@replace_top_n", n.ToString());
        try
        {
            var reader = cmd.ExecuteReader();
            result += "*Приглашенные игроки* \n";
            result += "```\n";
            result += "Т/И \t Игрок \n";

            while (reader.Read() && reader.HasRows)
            {
                var cntGame = Convert.ToInt32(reader["game_cnt"].ToString());
                var cntPractice = Convert.ToInt32(reader["practice_cnt"].ToString());
                var name = reader["name"].ToString()?.Replace("+", "");

                result += $"{cntPractice}/{cntGame} \t {name}\n";
            }

            result += "```";
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine(ex.Message);
        }
        return result;

    }

    private string GetInnerPlayersEventsStat(int n)
    {
        var result = "";
        var cmd = _conn.CreateCommand();
        //TODO move path to config
        cmd.CommandText = File.ReadAllText("./Database/get_players_event_count_inner.sql").Replace("@replace_top_n", n.ToString());

        try
        {
            var reader = cmd.ExecuteReader();
            result += "*Игроки из чата* \n";
            result += "```\n";
            result += "Т/И \t Игрок \n";
            while (reader.Read() && reader.HasRows)
            {
                var cntGame = Convert.ToInt32(reader["game_cnt"].ToString());
                var cntPractice = Convert.ToInt32(reader["practice_cnt"].ToString());
                //var username = reader["username"].ToString();
                var name = reader["name"].ToString();
                var surname = reader["surname"].ToString();

                //username = string.IsNullOrEmpty(username) ? "" : $"(@{username})";
                result += $"{cntPractice}/{cntGame} \t {name} {surname}\n";
            }
            result += "```";
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine(ex.Message);
        }
        return result;
    }
}