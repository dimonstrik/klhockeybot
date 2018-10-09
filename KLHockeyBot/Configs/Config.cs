using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System;

namespace KLHockeyBot.Configs
{
    public static class Config
    {
        public static readonly string BotToken = ConfigurationManager.AppSettings["BotToken"];

        public static readonly string DbSourceDirPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBSourceDirPath"];
        public static readonly string DbDirPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBDirPath"];
        public static readonly string DbPlayersPhotoDirPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBPlayersPhotoDirPath"];
        public static readonly string DbPlayersInfoPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBPlayersInfoPath"];
        public static readonly string DbEventsInfoPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBEventsInfoPath"];        
        public static readonly string DbGamesInfoPath = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBGamesInfoPath"];
        public static readonly string DbFile = Directory.GetCurrentDirectory() + ConfigurationManager.AppSettings["DBFile"];

        public static readonly string SportFortTeamMembersPage = ConfigurationManager.AppSettings["TeamMembersPage"];
        public static readonly string Pwd = ConfigurationManager.AppSettings["PWD"];

        public static class BotAdmin
        {
            private static readonly List<int> Admins = new List<int>();
            
            public static bool IsAdmin(int id)
            {
                return Admins.Contains(id);
            }

            static BotAdmin()
            {
                var keys = ConfigurationManager.AppSettings.AllKeys;
                foreach (var key in keys)
                {
                    try
                    {
                        if (key.Contains("Admin")) Admins.Add(int.Parse(ConfigurationManager.AppSettings[key]));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("BotAdmin initialization error: " + ex.Message);
                    }
                }
            }
        }
    }
}
